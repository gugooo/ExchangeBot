using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Threading;

namespace IExchange
{
    using CC_Interface = IExchange.MyClass.Client_Cod_Interface;
    public static class RMath
    {
        #region Interfaces
        public interface IExchange
        {
            bool ConectionStatus { get; }
            Task<List<MFiltre>> GetAllMarketsType();
            Task<List<IMarket>> GetAllMarkets(List<MFiltre> MarketsFiltre);
        }
        public enum IMarketStatus
        {
            Defoult,
            Runing,
            GoodMarket,
            Waiting,
            CanBeWaiting,
            Stoped,
            Stoping,
            BadMarket
        }
        public interface IMarket: IComparable, IEquatable<IMarket>
        {
            int RowCount { get; }
            List<Row> MainMarketRowsData { get; set; }
            string ID { get; }
            IMarketStatus IStatus { get; set; }
            bool IStatusChanged { get; set; }
            Dictionary<int, double> MarketProfitPercent(bool BestOrAll, List<Row> Rows = null);
            Dictionary<int, double> LastMarketProfitPercent { get; }
            Task<bool> PlaceBet(int row, bool BackOrLay, double kc, double stake);//xaxadruyq katarelu hamar
            Task<ORDER> PlaceBetO(int row3, bool BackOrLay, double kc, double stake);
            double? getBestKC(int row5);
            bool Stop();
            double nextKC(double kc, bool BackOrLay = true);
            Task<List<Row>> BestRows();
            List<Row> Last_BestRows { get; }
            DateTime BestRows_UpdateTime { get; }
            Task<List<Row>> AllRows();
            List<Row> Last_AllRows { get; }
            DateTime AllRows_UpdateTime { get; }
            MFiltre MF();
            Task<IEOrders> MarketOrders();
        }
        #region Order
        public interface ORDER
        {
            int Row3 { get; }
            int Row5 { get; }
            double OrderKC { get; }
            double OrderSize { get; }
            bool BackOrLay { get; }
            DateTime PlacedData { get; }
            double? AllMatchedSize { get; }
            double? NewMatchedSize { get; }
            void NewMatchedSizeIzComplate();
            Task<bool> UpdateOrder();
            Task<bool> CencelOrder(double? stake = null);
            Task<bool> ReplaseOrder(double KC);
        }
        public interface IEOrders
        {
            Dictionary<long, double> MarketProfit { get; }
            List<IEOrderMatched> Matched { get; }
            List<IEOrderUnMatched> UnMatched { get; }
            Task<bool> Refresh();
            Task<bool> CanselAllUnMatchedOrders();
        }
        public interface IEOrderUnMatched : IEOrderMatched
        {
            /// <summary>
            /// Veradarcnum e hanvac gumari chap@,
            /// "null" veradarcnume anhajox operaciayi depqum,
            /// "Size"-@ chnshelu depqum amboxch gumar@ hanelu porc e arvum,
            /// </summary>
            /// <param name="Stake">Hanvox gumari chap</param>
            Task<double?> CanselOrder(double? Stake = null);
            /// <summary>
            /// Poxum e gorcakic@
            /// </summary>
            /// <param name="Kc">Nor gorcakic</param>
            Task<bool> ReplaceOrder(double Kc);
        }
        public interface IEOrderMatched
        {
            /// <summary>
            /// Gorcarqi ID-n
            /// </summary>
            string OrderID { get; }
            /// <summary>
            /// Gorcarq@ katarelu jamanak@
            /// </summary>
            DateTime PlacedTime { get; }
            /// <summary>
            /// Marketi Toxi ID-n
            /// </summary>
            long RowID { get; }
            /// <summary>
            /// [Back:true],[Lay:false]
            /// </summary>
            bool BackOrLay { get; }
            double Kc { get; }
            double Stake { get; }
        }
        #endregion
        #endregion
        #region Classes
        public class Row
        {
            private Row() { }
            public Row(List<KcAndStake> back, List<KcAndStake> lay, double? ht = null)
            {
                this.back = back;
                this.lay = lay;
                this.ht = ht;
            }
            private List<KcAndStake> back;
            public List<KcAndStake> Back { get { return back; } }
            private List<KcAndStake> lay;
            public List<KcAndStake> Lay { get { return lay; } }
            private double? ht;
            public double? HT { get { return ht; } }
            public class KcAndStake
            {
                private KcAndStake() { }
                public KcAndStake(double kc, double stake)
                {
                    this.kc = kc;
                    this.stake = stake;
                }
                private double kc;
                public double Kc { get { return kc; } }
                private double stake;
                public double Stake { get { return stake; } }
            }
        }
        public class MFiltre
        {
            private MFiltre() { }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="MNane">Marketi tipi anun@</param>
            /// <param name="RCount">Marketi toxeri qanak@</param>
            /// <param name="WCount">Grupayi haxtoxneri qanak@</param>
            /// <param name="BT">Handicap=true, Total=false</param>
            public MFiltre(string MNane, string SportID, string SportName, int ActivRCount, int RCount, int WCount, BetingType BT)
            {
                this.marketTypeName = MNane;
                this.sportID = SportID;
                this.sportName = SportName;
                this.activeRowsCount = ActivRCount;
                this.rowsCount = RCount;
                this.winersCount = WCount;
                this.beting_Type = BT;
            }
            private string sportID;
            public string SportID { get { return sportID; } }

            private string sportName;
            public string SportName { get { return sportName; } }

            private string marketTypeName;
            public string MarketTypeName { get { return marketTypeName; } }

            private int activeRowsCount;
            public int ActiveRowsCout { get { return activeRowsCount; } }

            private int rowsCount;
            public int RowsCount { get { return rowsCount; } }

            private int winersCount;
            public int WinersCount { get { return winersCount; } }
            public enum BetingType
            {
                ODDS = 1,
                AH_S,
                AH_D
            }
            private BetingType beting_Type;
            public BetingType Beting_Type { get { return beting_Type; } }
        }
        #endregion
        private static bool End = true;
        public static void Initialize(List<IExchange> IES)
        {
            foreach (var el in IES)
            {
                Leech(el);
            }
        }
        #region Leech Global Data
        static List<IMarket> GIgnoringMarkets = new List<IMarket>();
        static List<IMarket> WaitingMarkets = new List<IMarket>();
        static List<IMarket> RuningMarkets = new List<IMarket>();
        static IMarket[] GAllMarkets = null;
        static bool GetAllMarketsUpdating = false;
        #endregion
        private static async Task Leech(IExchange Ex)
        {
            for (; !Ex.ConectionStatus; Thread.Sleep(500)) ;
            
            GAllMarkets = await getAllIMarkets(Ex);
            int TimerInterval=CC_Interface.Interval_GetAllMarkets * 60 * 1000;
            System.Timers.Timer Timer_getAllMarkets = new System.Timers.Timer(TimerInterval);
            Timer_getAllMarkets.Elapsed += async (a, b) =>
            {
                if (GetAllMarketsUpdating) return;
                try
                {
                    GetAllMarketsUpdating = true;
                    GAllMarkets = await getAllIMarkets(Ex);
                    GetAllMarketsUpdating = false;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Timer_getAllMarkets");
                }
                if (TimerInterval != CC_Interface.Interval_GetAllMarkets) Timer_getAllMarkets.Interval = TimerInterval = CC_Interface.Interval_GetAllMarkets * 60 * 1000;
            };
            Timer_getAllMarkets.Start();

            BookmekersControler();
          
        }
        async static void BookmekersControler()
        {
            #region Starting
            for (; GAllMarkets == null || GAllMarkets.Length == 0; Task.Delay(1000).Wait()) ;
            RuningMarkets = GAllMarkets.ToList();
            GAllMarkets = null;
            int RuningMarketsCount = CC_Interface.RunigMarketsCount;
            if (RuningMarkets.Count > RuningMarketsCount)
            {
                RuningMarkets.Take(RuningMarketsCount).ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
                RuningMarkets.Skip(RuningMarketsCount).ToList().ForEach(_ => _.IStatus = IMarketStatus.Waiting);
            }
            else RuningMarkets.ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
            WaitingMarkets.AddRange(RuningMarkets.Where(_ => _.IStatus == IMarketStatus.Waiting));
            RuningMarkets = RuningMarkets.Where(_ => _.IStatus == IMarketStatus.Runing).ToList();
            Dictionary<IMarket, CancellationTokenSource> Tokens = RuningMarkets.ToDictionary(_ => _, _ => new CancellationTokenSource());
            RuningMarkets.ForEach(_ => Task.Factory.StartNew(() => RobotBookmaker(_, Tokens.First(a => a.Key == _).Value.Token), TaskCreationOptions.LongRunning));
            double MinPercent = CC_Interface.MinProfitPercent;
            double TotalAvailable = CC_Interface.TotalAvailable;
            #endregion
            for (; ;await Task.Delay(10))
            {
                #region Runing Markets Count is changed
                if (RuningMarketsCount != CC_Interface.RunigMarketsCount) 
                {
                    if (RuningMarketsCount > CC_Interface.RunigMarketsCount)
                    {

                        var temp = RuningMarkets.Where(_ => _.IStatus == IMarketStatus.CanBeWaiting || _.IStatus == IMarketStatus.Runing || _.IStatus == IMarketStatus.GoodMarket).ToList();
                        if (CC_Interface.RunigMarketsCount - temp.Count >= 0) 
                        {
                            RuningMarketsCount = CC_Interface.RunigMarketsCount;
                            continue;
                        }
                        temp.Sort((a, b) =>//CanBeWaiting-Runing-GoodMarket
                        {
                            if (a.IStatus == b.IStatus) return 0;
                            if (a.IStatus == IMarketStatus.CanBeWaiting || (a.IStatus == IMarketStatus.Runing && b.IStatus != IMarketStatus.CanBeWaiting)) return -1;
                            return 1;
                        });
                        int StopedMarketsCount = temp.Count - CC_Interface.RunigMarketsCount;
                        if (temp.Count() > 0)
                        {
                            for (int i = 0; StopedMarketsCount >= 0; i++, StopedMarketsCount--)
                            {
                                temp[i].IStatus = IMarketStatus.Stoping;
                                temp[i].IStatusChanged = true;
                            }
                        }
                    }
                    else  WaitingMarkets.Take(CC_Interface.RunigMarketsCount - RuningMarketsCount).ToList().ForEach(_ => { _.IStatus = IMarketStatus.Runing; _.IStatusChanged = true; });
                    RuningMarketsCount = CC_Interface.RunigMarketsCount;
                }
                #endregion
                #region Have New GAllMarkets
                if (GAllMarkets != null && GAllMarkets.Length > 0)
                {
                    GAllMarkets.ToList().ForEach(_ => _.IStatusChanged = true);
                    if (RuningMarkets.Count < RuningMarketsCount)
                    {
                        if (RuningMarketsCount - RuningMarkets.Count >= GAllMarkets.Length)
                        {
                            GAllMarkets.ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
                            RuningMarkets.AddRange(GAllMarkets);
                            GAllMarkets = null;
                            continue;
                        }
                        else
                        {

                            GAllMarkets.Take(RuningMarketsCount - RuningMarkets.Count).ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
                            GAllMarkets = GAllMarkets.Skip(RuningMarketsCount - RuningMarkets.Count).ToArray();
                            GAllMarkets.ToList().ForEach(_ => _.IStatus = IMarketStatus.Waiting);
                        }
                    }
                    if (RuningMarkets.Any(_ => _.IStatus == IMarketStatus.CanBeWaiting))
                    {
                        if (RuningMarkets.Count(_ => _.IStatus == IMarketStatus.CanBeWaiting) >= GAllMarkets.Length)
                        {
                            RuningMarkets.Where(_ => _.IStatus == IMarketStatus.CanBeWaiting).Take(GAllMarkets.Length).ToList().ForEach(_ => { _.IStatusChanged = true; _.IStatus = IMarketStatus.Stoping; });
                            GAllMarkets.ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
                            RuningMarkets.AddRange(GAllMarkets);
                            GAllMarkets = null;
                            continue;
                        }
                        int count = 0;
                        RuningMarkets.Where(_ => _.IStatus == IMarketStatus.CanBeWaiting).ToList().ForEach(_ => { _.IStatusChanged = true; _.IStatus = IMarketStatus.Stoping; count++; });
                        GAllMarkets.Take(count).ToList().ForEach(_ => _.IStatus = IMarketStatus.Runing);
                        GAllMarkets.Skip(count).ToList().ForEach(_ => _.IStatus = IMarketStatus.Waiting);
                        RuningMarkets.AddRange(GAllMarkets);
                        GAllMarkets = null;
                        continue;
                    }
                    else
                    {
                        GAllMarkets.ToList().ForEach(_ => _.IStatus = IMarketStatus.Waiting);
                        RuningMarkets.AddRange(GAllMarkets);
                        GAllMarkets = null;
                        continue;
                    }
                }
                #endregion
                #region Market Status Changed
                if (RuningMarkets.Any(_ => _.IStatusChanged))
                {
                    IEnumerable<IMarket> ChangedMarkets = RuningMarkets.Where(_ => _.IStatusChanged == true);
                    #region BadMarkets
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.BadMarket))//BadMarkets
                    {
                        var BadMarkets = ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.BadMarket);
                        BadMarkets.ToList().ForEach(_ => _.IStatusChanged = false);
                        GIgnoringMarkets.AddRange(BadMarkets);
                        foreach (var el in BadMarkets) RuningMarkets.Remove(el);
                        GIgnoringMarkets.Sort();
                        WaitingMarkets.Take(BadMarkets.Count()).ToList().ForEach(_ => { _.IStatusChanged = true; _.IStatus = IMarketStatus.Runing; });
                    }
                    #endregion
                    #region Stoping
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.Stoping))//Stoping
                    {
                        ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.Stoping).ToList().ForEach(_ =>
                        {
                            _.IStatusChanged = false;
                            Tokens.Where(t => t.Key == _).ToArray()[0].Value.Cancel();
                        });
                    }
                    #endregion
                    #region Stoped
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.Stoped))//Stoped
                    {
                        var sm = ChangedMarkets.Where(_ =>_.IStatus == IMarketStatus.Stoped).ToList();
                        sm.ForEach(_ => { _.IStatus = IMarketStatus.Waiting; _.IStatusChanged = false; Tokens.Remove(_); RuningMarkets.Remove(_); });
                        WaitingMarkets.AddRange(sm);
                        WaitingMarkets.Sort();
                    }
                    #endregion
                    #region Waiting
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.Waiting))//Waiting
                    {
                        ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.Waiting).ToList().ForEach(_ =>
                        {
                            _.IStatusChanged = false;
                            RuningMarkets.Remove(_);
                            WaitingMarkets.Add(_);
                        });
                        WaitingMarkets.Sort();
                    }
                    #endregion
                    #region CanBeWaiting
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.CanBeWaiting))//CanBeWaiting
                    {
                        if (WaitingMarkets.Count > 0)
                        {
                            var wi = ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.CanBeWaiting).ToList();
                            WaitingMarkets.Take(wi.Count()).ToList().ForEach(_ =>
                            {
                                _.IStatusChanged = true;
                                _.IStatus = IMarketStatus.Runing;
                                var tw = wi.First();
                                tw.IStatus = IMarketStatus.Waiting;
                                tw.IStatusChanged = false;
                                WaitingMarkets.Add(tw);
                                RuningMarkets.Remove(tw);
                                wi.Remove(tw);
                            });
                            WaitingMarkets.Sort();
                            wi.ForEach(_ => _.IStatusChanged = false);
                        }
                        ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.CanBeWaiting).ToList().ForEach(_ => _.IStatusChanged = false);
                    }
                    #endregion
                    #region Runing
                    if (ChangedMarkets.Any(_ => _.IStatus == IMarketStatus.Runing))//Runing
                    {
                        ChangedMarkets.Where(_ => _.IStatus == IMarketStatus.Runing).ToList().ForEach(_ =>
                        {
                            _.IStatusChanged = false;
                            CancellationTokenSource t = new CancellationTokenSource();
                            Tokens.Add(_, t);
                            Task.Factory.StartNew(() => RobotBookmaker(_, t.Token));
                        });
                    }
                    #endregion
                }
                #region Waiting Markets
                if (WaitingMarkets.Count > 0)
                {
                    if (WaitingMarkets.Any(_ => _.IStatus == IMarketStatus.Runing))
                    {
                        var rm = WaitingMarkets.Where(_ => _.IStatus == IMarketStatus.Runing);
                        RuningMarkets.AddRange(rm);
                        foreach (var el in rm) WaitingMarkets.Remove(el);
                        RuningMarkets.Sort();
                    }
                    if (RuningMarkets.Count < RuningMarketsCount)
                    {
                        var rm = WaitingMarkets.Take(RuningMarketsCount - RuningMarkets.Count).ToList();
                        if (rm.Count() > 0)
                        {
                            rm.ToList().ForEach(_ => { _.IStatus = IMarketStatus.Runing; _.IStatusChanged = true; });
                            RuningMarkets.AddRange(rm);
                            foreach (var el in rm) WaitingMarkets.Remove(el);
                            RuningMarkets.Sort();
                        }
                    }
                    List<Task> UpdateWaitingMarkets = null;
                    bool WaitingMarketsSort = false;
                    WaitingMarkets.ForEach(_ =>
                    {
                        
                        if ((DateTime.Now - _.BestRows_UpdateTime).Minutes > 3)
                        {
                            UpdateWaitingMarkets = new List<Task>();
                            var __ = _;
                            var OldPercent = __.LastMarketProfitPercent.Sum(a => a.Value);
                            UpdateWaitingMarkets.Add(__.BestRows().ContinueWith(a =>
                            {
                                if (__.MarketProfitPercent(true) == null || __.LastMarketProfitPercent.Count == 0) WaitingMarkets.Remove(__);
                                else if (Math.Abs(OldPercent - __.LastMarketProfitPercent.Sum(b => b.Value)) / OldPercent > 0.001) WaitingMarketsSort = true;
                            }));
                        }
                    });
                    if (UpdateWaitingMarkets != null) Task.WaitAll(UpdateWaitingMarkets.ToArray());
                    if (WaitingMarketsSort) WaitingMarkets.Sort();
                }
                #endregion
                #endregion
            }
        }
        static async Task<IMarket[]> getAllIMarkets(IExchange Ex)
        {
            #region Markets Type
            List<MFiltre> AllMarketsType = await Ex.GetAllMarketsType();
            IEnumerable<MFiltre> FMarketsType = AllMarketsType.Where(_ => _.RowsCount == 3 && _.WinersCount == 1);
            #endregion
            #region Markets
            List<IMarket> AllMarkets = await Ex.GetAllMarkets(FMarketsType.ToList());
            if (AllMarkets == null)
            {
                return null;
            }
            var exeptedMarkets = GIgnoringMarkets.Concat(WaitingMarkets).Concat(RuningMarkets).Concat(GIgnoringMarkets);
            AllMarkets = AllMarkets.Except(exeptedMarkets).Except(AllMarkets.Where(_ => _.RowCount != 3)).ToList();
            List<IMarket> BestMarketsL1 = new List<IMarket>();
            Task<List<Row>>[] ResTasks = new Task<List<Row>>[AllMarkets.Count];
            DateTime time1 = DateTime.Now;
            for (int i = 0; i < AllMarkets.Count; i++)
            {
                ResTasks[i] = AllMarkets[i].BestRows();
            }
            Task.WaitAll(ResTasks);
            TimeSpan time2 = DateTime.Now - time1;
            try
            {
                for (int i = 0; i < ResTasks.Length; i++)
                {

                    #region Get Task Result
                    List<Row> result = ResTasks[i].Result;
                    if (result == null || result.Count == 0
                        || result.Any(_ => _ == null)
                        || result.Any(_ => _.Back == null || _.Back.Count == 0 || _.Lay == null || _.Lay.Count == 0)) continue;
                    if (result.Any(_ => _.Back.Any(__ => __ == null || __.Kc < 1) || _.Lay.Any(__ => __ == null || __.Kc < 1))) continue;
                    if (result.Count != 3) continue;
                    #endregion
                    #region Level 1
                    if (AllMarkets[i].MarketProfitPercent(true) != null && AllMarkets[i].LastMarketProfitPercent.Count > 0) 
                        {
                            BestMarketsL1.Add(AllMarkets[i]);
                        }
                    /*IDictionary<int, double> temp = Level_1R3(AllMarkets[i]);
                    if (temp.Count > 0) BestMarketsL1.Add(AllMarkets[i], temp.Select(_ => _.Value).Max());*/
                    #endregion
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            #region Best Markets Filtr

            /*var L1 = BestMarketsL1.ToList();
            if (BestMarketsL1.Count > 0) L1.Sort((a, b) => b.Value.CompareTo(a.Value));*/
            #endregion
            #endregion
            //return (L1.Select(_ => _.Key).ToArray());
            BestMarketsL1.Sort();
            return BestMarketsL1.ToArray();
        } 
        static IDictionary<int, double> Level_1R3(IMarket market, bool BestRows_Or_AllRows = true)//BestRows=true
        {
            double kc1, kc2, kc3, l1, l2, l3;
            if (BestRows_Or_AllRows)//BestRows
            {
                kc1 = market.Last_BestRows[0].Back[0].Kc;
                l1 = market.Last_BestRows[0].Lay[0].Kc;
                kc2 = market.Last_BestRows[1].Back[0].Kc;
                l2 = market.Last_BestRows[1].Lay[0].Kc;
                kc3 = market.Last_BestRows[2].Back[0].Kc;
                l3 = market.Last_BestRows[2].Lay[0].Kc;
            }
            else//AllRows
            {
                kc1 = market.Last_AllRows[0].Back[0].Kc;
                l1 = market.Last_AllRows[0].Lay[0].Kc;
                kc2 = market.Last_AllRows[1].Back[0].Kc;
                l2 = market.Last_AllRows[1].Lay[0].Kc;
                kc3 = market.Last_AllRows[2].Back[0].Kc;
                l3 = market.Last_AllRows[2].Lay[0].Kc;
            }
            double[] L1Percent = new double[6]{

            //Back KC
             profitPercent(market.nextKC(kc1, true), kc2, kc3),//KC1 - 0
             profitPercent(kc1, market.nextKC(kc2, true), kc3),//KC2 - 1
             profitPercent(kc1, kc2, market.nextKC(kc3, true)),//KC3 - 2

            //Lay KC
             profitPercent(market.nextKC(l1, false), l2, l3, false),//L1 - 3
             profitPercent(l1, market.nextKC(l2, false), l3, false),//L2 - 4
             profitPercent(l1, l2, market.nextKC(l3, false), false),//L3 - 5
            };
            IDictionary<int, double> res = new Dictionary<int, double>();
            for (int i = 0; i < 6; i++)
            {
                if (L1Percent[i] > CC_Interface.MinProfitPercent) res.Add(i, L1Percent[i]);
            }
            return res;
        }
        static async void RobotBookmaker(IMarket marketRef,CancellationToken token)
        {
            try
            {
                #region Update Market
                await marketRef.BestRows();
                var percentsInRows = marketRef.MarketProfitPercent(true);
                if (percentsInRows == null || percentsInRows.Count == 0) 
                {
                    marketRef.IStatusChanged = true;
                    marketRef.IStatus = IMarketStatus.Stoped;
                    return;
                }
                marketRef.MainMarketRowsData = marketRef.Last_BestRows;
                #endregion
                List<ORDER> ORDERS = new List<ORDER>();
                try
                {
                    #region Place bet
                    List<Task<ORDER>> OrderTasks = new List<Task<ORDER>>();
                    Dictionary<int, double> MaxStake = GetMaxStake(marketRef, CC_Interface.MarketRiskLimit);
                    foreach (var el in marketRef.LastMarketProfitPercent)
                    {
                        double kc = marketRef.nextKC((double)marketRef.getBestKC(el.Key), el.Key < 3 ? true : false);
                        double place = el.Key < 3 ? MaxStake[el.Key] : MaxStake[el.Key] / (kc - 1);
                        OrderTasks.Add(marketRef.PlaceBetO(el.Key % 3, el.Key > 2 ? false : true, kc, place));
                    }
                    Task.WaitAll(OrderTasks.ToArray());
                    for (int i = 0; i < OrderTasks.Count; i++)
                    {
                        if (OrderTasks[i].Result == null)
                        {
                            MessageBox.Show("");
                        }
                        ORDERS.Add(OrderTasks[i].Result);
                    }
                    #endregion
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message);
                }

                #region Control Bets
                List<Task<bool>> UpdateTasks = new List<Task<bool>>();
                for (; ORDERS.Count != 0; )
                {
                     #region Update Orders And Market
                     Task<List<Row>> UpBestRows = marketRef.BestRows();
                     #region Update Order
                    List<Task<bool>> UpOr = new List<Task<bool>>();
                    foreach (var el in ORDERS) UpOr.Add(el.UpdateOrder());
                    Task.WaitAll(UpOr.ToArray());
                    for (int i = 0; i < UpOr.Count; i++)
                    {
                        for (; !UpOr[i].Result; )
                        {
                            UpOr[i] = ORDERS[i].UpdateOrder();
                            if (await UpOr[i]) break;
                            DialogResult Dres = MessageBox.Show("Can't update Orders.", "RobotBookmaker Warning", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                            if (Dres == DialogResult.Retry) continue;
                            ORDERS.ForEach(_ => _.CencelOrder());
                            marketRef.IStatus = IMarketStatus.Stoped;
                            marketRef.IStatusChanged = true;
                            return;
                        }
                    }
                    #endregion
                     #region BestRows Update
                    await UpBestRows;
                    for (; UpBestRows.Result == null || marketRef.MarketProfitPercent(true) == null; ) 
                    {
                        UpBestRows = marketRef.BestRows();
                        if (await UpBestRows != null && marketRef.MarketProfitPercent(true) != null) break;
                        DialogResult Dres = MessageBox.Show("Can't update Best Rows.", "RobotBookmaker Warning", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                        if (Dres == DialogResult.Retry) continue;
                        ORDERS.ForEach(_ => _.CencelOrder());
                        marketRef.IStatus = IMarketStatus.Stoped;
                        marketRef.IStatusChanged = true;
                        return;
                    }
                    #endregion
                     #region ChashOut
                    List<ORDER> removingOrders = new List<ORDER>();
                        foreach (var el in ORDERS)
                        {
                            if (el.NewMatchedSize != null)
                            {
                                if(!await ChashOut(el, marketRef))
                                {
                                    marketRef.IStatus = IMarketStatus.BadMarket;
                                    marketRef.IStatusChanged = true;
                                    return;
                                }
                                el.NewMatchedSizeIzComplate();
                                if (el.AllMatchedSize.Value + 0.001 > el.OrderSize) removingOrders.Add(el);
                            }
                        }
                        foreach (var el in removingOrders) ORDERS.Remove(el);
                        #endregion
                     #region MarketProfitPercent == 0
                        if (marketRef.LastMarketProfitPercent.Count == 0)
                        {
                            List<Task> CancelOrders = new List<Task>();
                            for (int i = 0; i < ORDERS.Count; i++) CancelOrders.Add(ORDERS[i].CencelOrder());
                            Task.WaitAll(CancelOrders.ToArray());
                            ORDERS.Clear();
                            break;
                        }
                        #endregion
                     #region Market Stake Changed
                        bool ChangeMainMarket = false;
                        for (int i = 0; i < 3; i++)
                        {
                            if (Math.Round(marketRef.MainMarketRowsData[i].Back[0].Stake, 2) != Math.Round(marketRef.Last_BestRows[i].Back[0].Stake, 2)
                             || Math.Round(marketRef.MainMarketRowsData[i].Lay[0].Stake, 2) != Math.Round(marketRef.Last_BestRows[i].Lay[0].Stake, 2))
                            {
                                ChangeMainMarket = true;
                                #region Cancal And Place Order
                                IEnumerable<int> keys=null;// = marketRef.LastMarketProfitPercent.Select(_ => _.Key).Except(ORDERS.Select(_ => _.Row5));//Get Censaled and Placed new orders
                                keys = marketRef.LastMarketProfitPercent.Select(_ => _.Key).Except(ORDERS.Select(_ => _.Row5));//Get Censaled and Placed new orders
                                if (keys.Count() != 0)
                                {
                                    foreach(var kay  in keys)
                                    {
                                        if (ORDERS.Any(__ => __.Row5 == kay))//Cansaled
                                        {
                                            var CancaledOrders = ORDERS.Where(o => o.Row5 == kay);
                                            List<Task> CT = new List<Task>();
                                            foreach (var el in CancaledOrders) CT.Add(el.CencelOrder());
                                            Task.WaitAll(CT.ToArray());
                                            foreach (var el in CancaledOrders) ORDERS.Remove(el);

                                        }
                                        else//Placed
                                        {
                                            double PlacedKc = marketRef.nextKC((double)marketRef.getBestKC(kay), kay > 2 ? false : true);
                                            double PlacedStake = GetMaxStake(marketRef, CC_Interface.MarketRiskLimit)[kay];
                                            ORDERS.Add(await marketRef.PlaceBetO(kay % 3, kay < 3 ? true : false, PlacedKc, kay < 3 ? PlacedStake : PlacedStake / (PlacedKc - 1)));
                                        }
                                    }
                                }
                                #endregion
                                #region Update Stakes
                                List<Task<ORDER>> placeTasks = new List<Task<ORDER>>();
                                List<Task> cancelTask = new List<Task>();
                                foreach (var RMaxStake in GetMaxStake(marketRef, CC_Interface.MarketRiskLimit))
                                {
                                    List<ORDER> temp = ORDERS.Where(_ => _.Row5 == RMaxStake.Key).ToList();
                                    double RuningOrderStake = Math.Round(temp.Sum(_ => _.OrderSize - (_.AllMatchedSize == null ? 0.0 : (double)_.AllMatchedSize)), 2);
                                    if (RMaxStake.Key > 2) RuningOrderStake = Math.Round(RuningOrderStake * (temp[0].OrderKC - 1), 2);
                                    double MaxStake = Math.Round(RMaxStake.Value, 2);
                                    if (RuningOrderStake != MaxStake && Math.Round(Math.Abs(RuningOrderStake - MaxStake), 2) > 0.1) 
                                    {
                                        temp.Sort((a, b) =>
                                        {
                                            if (a.OrderSize - (a.AllMatchedSize.HasValue ? a.AllMatchedSize.Value : 0) > b.OrderSize - (b.AllMatchedSize.HasValue ? b.AllMatchedSize.Value : 0)) return 1;
                                            if (a.OrderSize - (a.AllMatchedSize.HasValue ? a.AllMatchedSize.Value : 0) < b.OrderSize - (b.AllMatchedSize.HasValue ? b.AllMatchedSize.Value : 0)) return -1;
                                            return 0;
                                        });
                                        if (RuningOrderStake < MaxStake)
                                        {
                                            try
                                            {
                                                placeTasks.Add(marketRef.PlaceBetO(temp[0].Row3, temp[0].BackOrLay, temp[0].OrderKC, Math.Round(temp[0].BackOrLay ? (MaxStake - RuningOrderStake) : (RMaxStake.Value - RuningOrderStake) / (temp[0].OrderKC - 1), 2)));
                                            }
                                            catch (Exception e)
                                            {
                                                MessageBox.Show("");
                                            }
                                            continue;
                                        }
                                        double CancalStakeValue = RuningOrderStake - MaxStake;
                                        foreach (var or in temp)
                                        {
                                            double OrderSize = or.BackOrLay ? or.OrderSize - (or.AllMatchedSize == null ? 0 : (double)or.AllMatchedSize) : (or.OrderSize - (or.AllMatchedSize == null ? 0 : (double)or.AllMatchedSize)) * (or.OrderKC - 1);
                                            if (Math.Round(OrderSize, 2) <= Math.Round(CancalStakeValue, 2)) 
                                            {
                                                cancelTask.Add( or.CencelOrder());
                                                ORDERS.Remove(or);
                                                CancalStakeValue -= Math.Round(OrderSize, 2);
                                                continue;
                                            }
                                            if (Math.Round(CancalStakeValue, 2) == 0.00) break;
                                            cancelTask.Add(or.CencelOrder(Math.Round(or.BackOrLay ? CancalStakeValue : (CancalStakeValue / (or.OrderKC - 1)), 2)));
                                            break;
                                        }
                                    }
                                }
                                if (placeTasks.Count > 0)
                                {
                                    Task.WaitAll(placeTasks.ToArray());
                                    foreach (var plOr in placeTasks) ORDERS.Add(plOr.Result);
                                }
                                if (cancelTask.Count > 0) Task.WaitAll(cancelTask.ToArray());
                                #endregion
                                break;
                            }
                        }
                        #endregion
                     #region KC is changed
                        for (int i = 0; i < 3; i++)
                        {
                            if (Math.Round(marketRef.MainMarketRowsData[i].Back[0].Kc, 2) != Math.Round(marketRef.Last_BestRows[i].Back[0].Kc, 2)
                             || Math.Round(marketRef.MainMarketRowsData[i].Lay[0].Kc, 2) != Math.Round(marketRef.Last_BestRows[i].Lay[0].Kc, 2))
                            {
                                ChangeMainMarket = true;
                                foreach (var el in marketRef.LastMarketProfitPercent)
                                {
                                    double UpdatedKc = marketRef.nextKC((double)marketRef.getBestKC(el.Key), el.Key < 3 ? true : false);
                                    foreach (var or in ORDERS.Where(_ => _.Row5 == el.Key))
                                    {
                                        if (Math.Round(or.OrderKC, 2) != Math.Round(UpdatedKc, 2))
                                        {
                                            await or.ReplaseOrder(UpdatedKc);
                                        }
                                    }
                                }
                                break;
                            }
                        }
                        #endregion
                     if (ChangeMainMarket) marketRef.MainMarketRowsData = marketRef.Last_BestRows;
                     #endregion
                }
                marketRef.IStatus = IMarketStatus.Stoped;
                marketRef.IStatusChanged = true;
                #endregion
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "C[RMath]F[RobotBookmaker]");
            }
        }
        static Dictionary<int,double> GetMaxStake(IMarket mr,double? MarketLimit = null)
        {
            if (mr.LastMarketProfitPercent == null) mr.MarketProfitPercent(true);
            Dictionary<int, double> temp = new Dictionary<int, double>();
            foreach (var el in mr.LastMarketProfitPercent)
            {
                if (el.Key < 3)//Back
                {
                    double kc1 = (double)mr.getBestKC(0), kc2 = (double)mr.getBestKC(1), kc3 =(double) mr.getBestKC(2);
                    double SK1 = mr.Last_BestRows[0].Back[0].Stake * kc1;
                    double SK2 = mr.Last_BestRows[1].Back[0].Stake * kc2;
                    double SK3 = mr.Last_BestRows[2].Back[0].Stake * kc3;
                    switch (el.Key)
                    {
                        case 0: temp.Add(0, SK2 / kc1 < SK3 / kc1 ? SK2 / kc1 : SK3 / kc1);
                            break;
                        case 1: temp.Add(1, SK1 / kc2 < SK3 / kc2 ? SK1 / kc2 : SK3 / kc2);
                            break;
                        case 2: temp.Add(2, SK1 / kc3 < SK2 / kc3 ? SK1 / kc3 : SK2 / kc3);
                            break;
                    }
                }
                else//Lay
                {
                    double L1 = (double)mr.getBestKC(3), L2 = (double)mr.getBestKC(4), L3 = (double)mr.getBestKC(5);
                    double SL1 = mr.Last_BestRows[0].Lay[0].Stake * L1;
                    double SL2 = mr.Last_BestRows[0].Lay[1].Stake * L2;
                    double SL3 = mr.Last_BestRows[0].Lay[2].Stake * L3;
                    switch (el.Key)
                    {
                        case 3: temp.Add(3, (SL2 / L1 < SL3 / L1 ? SL2 / L1 : SL3 / L1) / (L1 - 1)); 
                            break;
                        case 4: temp.Add(4, (SL1 / L2 < SL3 / L2 ? SL1 / L2 : SL3 / L2) / (L2 - 1));
                            break;
                        case 5: temp.Add(5, (SL1 / L3 < SL2 / L3 ? SL1 / L3 : SL2 / L3) / (L3 - 1));
                            break;
                    }
                }
            }
            if (MarketLimit != null)
            {
                int[] Y = { 0, 0, 0 };
                foreach (var el in mr.LastMarketProfitPercent)
                {
                    switch (el.Key)
                    {
                        case 0: Y[1]++; Y[2]++; break;
                        case 1: Y[0]++; Y[2]++; break;
                        case 2: Y[0]++; Y[1]++; break;
                        case 3: Y[0]++; break;
                        case 4: Y[1]++; break;
                        case 5: Y[2]++; break;
                    }
                }
                double risk = (double)MarketLimit / Y.Max();
                var ptr = temp.ToList();
                for (int i = 0; i < ptr.Count; i++) 
                {

                    if (ptr[i].Value > risk) temp[ptr[i].Key] = risk;
                }
            }
            return temp;
        }
        async static Task<bool> ChashOut(ORDER or,IMarket mr)
        {
            if (or.NewMatchedSize != null)
            {
                double k1, k2, k3;
                if (or.BackOrLay)//Back
                {
                    k1 = or.Row3 == 0 ? or.OrderKC : mr.Last_BestRows[0].Back[0].Kc;
                    k2 = or.Row3 == 1 ? or.OrderKC : mr.Last_BestRows[1].Back[0].Kc;
                    k3 = or.Row3 == 2 ? or.OrderKC : mr.Last_BestRows[2].Back[0].Kc;
                    
                }
                else//Lay
                {
                    k1 = or.Row3 == 0 ? or.OrderKC : mr.Last_BestRows[0].Lay[0].Kc;
                    k2 = or.Row3 == 1 ? or.OrderKC : mr.Last_BestRows[1].Lay[0].Kc;
                    k3 = or.Row3 == 2 ? or.OrderKC : mr.Last_BestRows[2].Lay[0].Kc;
                }
                if (profitPercent(k1, k2, k3, or.BackOrLay) < 0)
                {
                    MessageBox.Show("ChashOut Percent Is < 0, MarketID: " + mr.ID + "\nBack Kc: " + Math.Round(or.OrderKC, 2).ToString() + " Stake: " + or.NewMatchedSize.Value.ToString(), "ChashOut Warning", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    or.NewMatchedSizeIzComplate();
                    return false;
                }
                var calc = CalcSize(mr, or.Row5, or.OrderKC, (double)or.NewMatchedSize);
                foreach (var el2 in calc) await mr.PlaceBetO(el2.Key % 3, el2.Key > 2 ? false : true, (double)mr.getBestKC(el2.Key), el2.Value);
                or.NewMatchedSizeIzComplate(); //NewMatchedSize = NULL
            }
            else return false;
            return true;
        }
        static List<KeyValuePair<int, double>> CalcSize(IMarket marketRef, int row5, double kc, double MatchedSize)
        {
            List<KeyValuePair<int, double>> temp = new List<KeyValuePair<int,double>>();
            for (int i = 0; i < 3; i++) if (row5 % 3 != i) temp.Add(new KeyValuePair<int, double>(row5 > 2 ? i + 3 : i, MatchedSize * kc / (row5 > 2 ? (double)marketRef.getBestKC(i + 3) : (double)marketRef.getBestKC(i))));
            #region Sort
            if (row5 < 3)
            {
                temp.ToList().Sort((a, b) =>
                {
                    if (a.Value > b.Value) return 1;
                    if (a.Value < b.Value) return -1;
                    return 0;
                });
            }
            else
            {
                temp.ToList().Sort((a, b) =>
                {
                    if (a.Value * marketRef.getBestKC(a.Key) > b.Value * marketRef.getBestKC(b.Key)) return 1;
                    if (a.Value * marketRef.getBestKC(a.Key) < b.Value * marketRef.getBestKC(b.Key)) return -1;
                    return 0;
                });
            }
            #endregion
            return  temp;
        }
        #region Profit Percent
        public static double profitPercent(double kc1, double kc2, double kc3, bool backOrLay = true)
        {
            if (backOrLay)
            {
                return 100 * ((kc1 * kc2 * kc3) / (kc1 * kc2 + kc2 * kc3 + kc3 * kc1) - 1);
            }
            double temp = kc1 * kc2 + kc2 * kc3 + kc3 * kc1 - kc1 * kc2 * kc3;
            return 100 * temp / (2 * kc3 * kc2 * kc1 - temp);
        }
        private static double? Percent(IEnumerable<Row> rows, bool BackOrLay = true)
        {
            if (rows == null) return null;

            double[] BackBestKcs = rows.Select(_ => _.Back[0].Kc).ToArray<double>();
            decimal top = 1;
            foreach (var kc in BackBestKcs) top *= (decimal)kc;

            decimal bottom = 0;
            for (int i = 0; i < BackBestKcs.Length; i++)
            {
                decimal temp = 1;
                for (int j = 0; j < BackBestKcs.Length; j++) if (j != i) temp *= (decimal)BackBestKcs[j];
                bottom += temp;
            }

            if (BackOrLay)//Back
            {
                return (double)(100 * top / (bottom - 1));
            }
            //Lay
            decimal tb = bottom - top;
            return (double)(100 * tb / (2 * top - tb));
        }
        #endregion
        public static void IEMathEnd() { End = false; }
    }
}
