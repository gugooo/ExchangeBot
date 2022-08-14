using System;
using System.Windows.Forms;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System.Data.Linq;
using System.Data.Linq.Mapping;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Security.Cryptography.X509Certificates;
using System.IO;
using System.Threading;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace IExchange
{
    #region ThreadOptimization
    public class ThreadOptimization<T1, T2>
        where T2 : class
    {
        private Func<T1[], Task<T2[]>> MainFunc;
        private Func<T1[], int> elementPointr;
        private Func<object, object, bool> propertyIsComplate;
        private int maxPointrInRequest;
        private TimerGetResp TResp;
        /// <summary>
        /// 
        /// </summary>
        /// <param name="OFunc">Glxavor funkcian, vor@ petqe opimizacnel</param>
        /// <param name="ElementPointr">Veradarcnum e argumentin hamapatasxan pointrneri qanak@</param>
        /// <param name="PropertyIsComplate">Havasarazor proprtineri depqum petqe veradarcni "True", hakarak depqum "False"</param>
        /// <param name="MaxPointrInRequest">1 requestin tuylatrvox pointrneri qanak</param>
        public ThreadOptimization(Func<T1[], Task<T2[]>> OFunc, Func<T1[], int> ElementPointr,Func<object,object,bool> PropertyIsComplate, int MaxPointrInRequest)
        {
            this.MainFunc = OFunc;
            this.maxPointrInRequest = MaxPointrInRequest;
            this.elementPointr = ElementPointr;
            this.propertyIsComplate = PropertyIsComplate;
            TResp = new TimerGetResp(10, getResp,LO);
        }
        private List<T1> Requests = new List<T1>();
        private List<T2> Responses = new List<T2>();
        private int pointrsCount = 0;
        private refBool LO = new refBool() { status = false };
        private  object LockOptimization = new object();
        private object LockResp = new object();
        private object LockReq = new object();
        private refBool HaveRespRef = new refBool();
        private async void getResp()
        {
            List<T1> Req;
            List<T2> tempResp;
            refBool RespStatus;
            //lock (this.LockResp)
            //{
            if (Requests.Count == 0) return;
            Req = new List<T1>(Requests);
            Requests = new List<T1>();
            RespStatus = HaveRespRef;
            HaveRespRef = new refBool();
            tempResp = Responses;
            Responses = new List<T2>();
            
            List<Lazy<T2[]>> WL = new List<Lazy<T2[]>>(WLazy);
            WLazy.Clear();
            pointrsCount = 0;
            //}
            if (Req.Count > maxPointrInRequest) throw new Exception("Error ThreadOpimization class: Request count");
            try
            {
                var TEMPR = await MainFunc(Req.ToArray());
                if (TEMPR == null || TEMPR.Count() == 0)
                {
                    TEMPR = await MainFunc(Req.ToArray());
                }
                if (TEMPR == null || TEMPR.Count() == 0)
                {
                    for (int i = 0; i < Req.Count; i++) tempResp.Add(null);
                }
                else tempResp.AddRange(TEMPR);
                if (Req.Count != tempResp.Count)
                {
                    MessageBox.Show("Ereor!");
                }
            }
            catch (Exception ex)
            {
                if (tempResp.Count < Req.Count) for (int i = 0, j = Req.Count - tempResp.Count; i < j; i++) tempResp.Add(null);
            }
            finally
            {
                RespStatus.status = true;
            }
        }
        List<Lazy<T2[]>> WLazy = new List<Lazy<T2[]>>();
        private Lazy<T2[]> addReq(T1[] a)
        {
            //TResp.Restart();
            lock (this.LockReq)
            {
                int ArgsCount = a.Length;
                int StartNum = Requests.Count;
                List<T2> RespRef = Responses;
                Lazy<T2[]> respLazy = new Lazy<T2[]>(() =>
                {

                    T2[] r = new T2[ArgsCount];
                    for (int i = StartNum, j = 0; j < ArgsCount; i++, j++)  r[j] = RespRef[i];//r[1],respRef[3],j=4<<--------------
                    return r;
                });
                Requests.AddRange(a);
                WLazy.Add(respLazy);
                return respLazy;
            }
        }
        List<T1[]> ReqGrupsCreator(T1[] a, int FirstFreeReq)
        {
            List<T1[]> resp = new List<T1[]>();
            List<T1> temp = new List<T1>();
            if (FirstFreeReq != maxPointrInRequest)
            {
                for (int i = 0, j = elementPointr(new T1[] { a[i] }); j <= FirstFreeReq; )
                {

                    temp.Add(a[i]);
                    if (++i < a.Length) j += elementPointr(new T1[] { a[i] });
                    else break;
                }
                resp.Add(temp.ToArray());
                a = a.Skip(temp.Count).ToArray();
                temp.Clear();
            }
            int p = 0, ep = 0;
            foreach (var el in a)
            {
                ep = elementPointr(new T1[] { el });
                p += ep;
                if (p < maxPointrInRequest) { temp.Add(el); continue; }
                else if (p == maxPointrInRequest)
                {
                    temp.Add(el);
                    resp.Add(temp.ToArray());
                    p = 0;
                    temp.Clear();
                    continue;
                }
                resp.Add(temp.ToArray());
                temp.Clear();
                temp.Add(el);
                p = ep;
            }
            if (temp.Count > 0) resp.Add(temp.ToArray());
            int eec = resp[resp.Count - 1].Length;
            if (eec < maxPointrInRequest) pointrsCount = eec;
            else pointrsCount = 0;
            return resp;
        }
        public OptimizedResp Optimization(T1[] args, object ReqProperty = null)
        {
            TResp.Restart();
            Lazy<T2[]> respLazy = null;
            OResp temp;
            lock (LockOptimization)
            {
                if (this.LO.status) for (; this.LO.status; Task.Delay(1).Wait()) ;
                pointrsCount += elementPointr(args);
                if (pointrsCount < maxPointrInRequest)
                {
                    respLazy = addReq(args);
                    temp = new OResp(respLazy, new refBool[] { HaveRespRef });
                }
                else if (pointrsCount == maxPointrInRequest)
                {
                    respLazy = addReq(args);
                    temp = new OResp(respLazy, new refBool[] { HaveRespRef });
                    getResp();
                }
                else
                {
                    List<Lazy<T2[]>> LS = new List<Lazy<T2[]>>();
                    List<refBool> AllRS = new List<refBool>();
                   // lock (LockOptimization)
                    //{

                        int FirstFreeReq = maxPointrInRequest - (pointrsCount - elementPointr(args));
                        List<T1[]> Reqs = ReqGrupsCreator(args, FirstFreeReq);

                        if (FirstFreeReq < elementPointr(new T1[] { args[0] })) getResp();

                        for (int i = 0; ; i++)
                        {
                            AllRS.Add(HaveRespRef);
                            LS.Add(addReq(Reqs[i]));
                            if (i == Reqs.Count - 1)
                            {
                                if (elementPointr(Reqs[i]) < maxPointrInRequest) break;
                                getResp();
                                break;
                            }
                            getResp();
                        }
                   // }
                    respLazy = new Lazy<T2[]>(() =>
                    {
                        List<T2> r = new List<T2>();
                        foreach (var el in LS) r.AddRange(el.Value);
                        return r.ToArray();
                    });
                    temp = new OResp(respLazy, AllRS.ToArray());
                }
                return temp;
            }
        }
        public class refBool
        {
            public bool status = false;
        }
        private class OResp : OptimizedResp
        {
            private Lazy<T2[]> oResp;
            private refBool[] HaveResp;
            public OResp(Lazy<T2[]> Resp, refBool[] respStatusRef)
            {
                this.oResp = Resp;
                this.HaveResp = respStatusRef;
            }
            T2[] OptimizedResp.Resp
            {
                get
                {
                    if (HaveResp.All(_ => _.status == true)) return this.oResp.Value;
                    return null;
                }
            }
            bool OptimizedResp.HaveResp
            {
                get { return this.HaveResp.All(_ => _.status == true); }
            }
            async Task OptimizedResp.WaitingResp()
            {
                for (; !((OptimizedResp)this).HaveResp; await Task.Delay(1)) ;
            }
        }
        public interface OptimizedResp
        {
            T2[] Resp { get; }
            bool HaveResp { get; }
            Task WaitingResp();
        }
        public class TimerGetResp
        {
            private int interval;
            private Action CB;
            private bool RStatus;
            private bool Started = true;
            private refBool ol;
            public TimerGetResp(int Interval, Action CallBack,refBool OL)
            {
                this.interval = Interval;
                this.CB = CallBack;
                this.ol = OL;
            }
            public void Restart()
            {
                lock (this)
                {
                    if (Started)
                    {
                        Started = false;
                        Task.Factory.StartNew(() => Start(), TaskCreationOptions.LongRunning);
                    }
                }
                RStatus = true;
            }
            async void Start()
            {
                for (int i = 0; ; i++)
                {
                    await Task.Delay(1);
                    if (RStatus)
                    {
                        i = -1;
                        RStatus = false;
                        continue;
                    }
                    if (i >= interval)
                    {
                        ol.status = true;
                        CB();
                        ol.status = false;
                        i = -1;
                    }
                }
            }
        }
    }
    #endregion
    #region Betfair
    public class Betfair
    {
        #region Results Classes
        [JsonConverter(typeof(StringEnumConverter))]
        public enum Wallet
        {
            UK = 1,
            AUSTRALIAN
        }
        public class AccountFundsResponse
        {
            public double availableToBetBalance { get; set; }
            public double exposure { get; set; }
            public double retainedCommission { get; set; }
            public double exposureLimit { get; set; }
            public double discountRate { get; set; }
            public int pointsBalance { get; set; }
        }
        public class VenueResult
        {
            public string venue { get; set; }
            public int marketCount { get; set; }
        }
        public class TimeRangeResult
        {
            public int? marketCount { get; set; }
            public TimeRange timeRange { get; set; }
        }
        public class EventTypeResult
        {
            public int? marketCount { get; set; }
            public EventType eventType { get; set; }
            public class EventType
            {
                public string id { get; set; }
                public string name { get; set; }
            }
        }
        public class CountryCodeResult
        {
            public string countryCode { get; set; }
            public int? marketCount { get; set; }
        }
        public class CompetitionResult
        {
            public Competition competition { get; set; }
            public int? marketCount { get; set; }
            public string competitionRegion { get; set; }
            public class Competition
            {
                public string name { get; set; }
                public string id { get; set; }
            }
        }
        public class EventResult
        {
            public Event @event { get; set; }
            public int? marketCount { get; set; }
            public class Event
            {
                public string name { get; set; }
                public string id { get; set; }
                public string countryCode { get; set; }
                public string timezone { get; set; }
                public string venue { get; set; }
                public DateTime? openDate { get; set; }
            }
        }
        public class MarketTypeResult
        {
            public string marketType { get; set; }
            public int? marketCount { get; set; }
        }
        public class MarketCatalogue
        {
            public string marketId { get; set; }
            public string marketName { get; set; }
            public DateTime? marketStartTime { get; set; }
            public MarketDescription description { get; set; }
            public double? totalMatched { get; set; }
            public List<RunnerCatalog> runners { get; set; }
            public EventTypeResult.EventType eventType { get; set; }
            public CompetitionResult.Competition competition { get; set; }
            public EventResult.Event @event { get; set; }
            public class MarketDescription
            {
                public bool persistenceEnabled { get; set; }
                public bool bspMarket { get; set; }
                public DateTime marketTime { get; set; }
                public DateTime suspendTime { get; set; }
                public DateTime? settleTime { get; set; }
                public MarketBettingType bettingType { get; set; }
                public bool turnInPlayEnabled { get; set; }
                public string marketType { get; set; }
                public string regulator { get; set; }
                public double marketBaseRate { get; set; }
                public bool discountAllowed { get; set; }
                public string wallet { get; set; }
                public string rules { get; set; }
                public bool? rulesHasDate { get; set; }
                public string clarifications { get; set; }
                [JsonConverter(typeof(StringEnumConverter))]
                public enum MarketBettingType
                {
                    ODDS=1,
                    LINE,
                    RANGE,
                    ASIAN_HANDICAP_DOUBLE_LINE,
                    ASIAN_HANDICAP_SINGLE_LINE,
                    FIXED_ODDS
                }
            }
            public class RunnerCatalog
            {
                public long selectionId { get; set; }
                public string runnerName { get; set; }
                public double handicap { get; set; }
                public int sortPriority { get; set; }
                public Dictionary<string, string> metadata { get; set; }
            }
        }
        public class Order
        {
            public string betId { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public enum OrderType
            {
                LIMIT=1,
                LIMIT_ON_CLOSE,
                MARKET_ON_CLOSE
            }
            public OrderType orderType { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public enum OrderStatus
            {
                EXECUTION_COMPLETE=1,
                EXECUTABLE
            }
            public OrderStatus status { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public enum PersistenceType
            {
                LAPSE=1,
                PERSIST,
                MARKET_ON_CLOSE
            }
            public PersistenceType persistenceType { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public enum Side
            {
                BACK=1,
                LAY
            }
            public Side side { get; set; }
            public double price { get; set; }
            public double size { get; set; }
            public double bspLiability { get; set; }
            public DateTime placedDate { get; set; }
            public double? avgPriceMatched { get; set; }
            public double? sizeMatched { get; set; }
            public double? sizeRemaining { get; set; }
            public double? sizeLapsed { get; set; }
            public double? sizeCancelled { get; set; }
            public double? sizeVoided { get; set; }
        }
        public class MarketBook
        {
            public string marketId { get; set; }
            public bool isMarketDataDelayed { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public enum MarketStatus
            {
                INACTIVE,
                OPEN,
                SUSPENDED,
                CLOSED
            }
            public MarketStatus? status { get; set; }
            public int? betDelay { get; set; }
            public bool? bspReconciled { get; set; }
            public bool? complete { get; set; }
            public bool? inplay { get; set; }
            public int? numberOfWinners { get; set; }
            public int? numberOfRunners { get; set; }
            public int? numberOfActiveRunners { get; set; }
            public DateTime? lastMatchTime { get; set; }
            public double? totalMatched { get; set; }
            public double? totalAvailable{get;set;}
            public bool? crossMatching { get; set; }
            public bool? runnersVoidable { get; set; }
            public long? version { get; set; }
            public class Runner
            {
                public long selectionId { get; set; }
                public double handicap { get; set; }
                [JsonConverter(typeof(StringEnumConverter))]
                public enum RunnerStatus
                {
                    ACTIVE=1,
                    WINNER,
                    LOSER,
                    REMOVED_VACANT,
                    REMOVED,
                    HIDDEN
                }
                public RunnerStatus status { get; set; }
                public double adjustmentFactor { get; set; }
                public double? lastPriceTraded { get; set; }
                public double? totalMatched { get; set; }
                public DateTime? removalDate { get; set; }
                public class StartingPrices
                {
                    public double? nearPrice { get; set; }
                    public double? farPrice { get; set; }
                    public List<PriceSize> backStakeTaken { get; set; }
                    public List<PriceSize> layLiabilityTaken { get; set; }
                    public double? actualSP { get; set; }
                }
                public StartingPrices sp { get; set; }
                public class ExchangePrices
                {
                    public List<PriceSize> availableToBack{get;set;}
                    public List<PriceSize> availableToLay{get;set;}
                    public List<PriceSize> tradedVolume{get;set;}
                }
                public ExchangePrices ex{get;set;}
                public List<Order> orders { get; set; }
                public class Match
                {
                    public string betId { get; set; }
                    public string matchId { get; set; }
                    public Order.Side side { get; set; }
                    public double price { get; set; }
                    public double size { get; set; }
                    public DateTime? matchDate { get; set; }
                }
                public List<Match> matches { get; set; }
            }
            public List<Runner> runners { get; set; }
        }
        public class PriceSize
        {
            public double price { get; set; }
            public double size { get; set; }
        }
        public class PlaceExecutionReport
        {
            public string customerRef { get; set; }
            public string marketId { get; set; }
            public ExecutionReportStatus status { get; set; }
            public ExecutionReportErrorCode? errorCode { get; set; }
            public class PlaceInstructionReport
            {
                public string betId { get; set; }
                public DateTime? placedDate { get; set; }
                public double? averagePriceMatched { get; set; }
                public double? sizeMatched { get; set; }
                public InstructionReportStatus status { get; set; }
                public InstructionReportErrorCode? errorCode { get; set; }
                public PlaceInstruction instruction { get; set; }
            }
            public PlaceInstructionReport[] instructionReports { get; set; }
        }
        public class PlaceInstruction
        {
            public long selectionId { get; set; }
            public double? handicap { get; set; }
            public LimitOrder limitOrder { get; set; }
            public LimitOnCloseOrder limitOnCloseOrder { get; set; }
            public MarketOnCloseOrder marketOnCloseOrder { get; set; }
            public Order.OrderType orderType { get; set; } 
            public Order.Side side { get; set; }
            public class LimitOrder
            {
                public double size { get; set; }
                public double price { get; set; }
                public Order.PersistenceType persistenceType { get; set; }
            }
            public class LimitOnCloseOrder
            {
                public double liability { get; set; }
                public double price { get; set; }
            }
            public class MarketOnCloseOrder
            {
                public double liability { get; set; }
            }
        }
        public class CancelExecutionReport
        {
            public string customerRef { get; set; }
            public ExecutionReportStatus status { get; set; }
            public ExecutionReportErrorCode? errorCode { get; set; }
            public string marketId { get; set; }
            public class CancelInstructionReport
            {
                public InstructionReportStatus status { get; set; }
                public InstructionReportErrorCode? errorCode { get; set; }
                public CancelInstruction instruction { get; set; }
                public double sizeCancelled { get; set; }
                public DateTime? cancelledDate { get; set; }
            }
            public CancelInstructionReport[] instructionReports { get; set; }
        }
        public class CancelInstruction
        {
            public string betId { get; set; }
            public double? sizeReduction { get; set; }
        }
        public class UpdateExecutionReport
        {
            public string customerRef { get; set; }
            public string marketId { get; set; }
            public ExecutionReportStatus status { get; set; }
            public ExecutionReportErrorCode? errorCode { get; set; }
            public class UpdateInstructionReport
            {
                public InstructionReportStatus status { get; set; }
                public InstructionReportErrorCode? errorCode { get; set; }
                public UpdateInstruction instruction { get; set; }
            }
            public UpdateInstructionReport[] instructionReports { get; set; }
        }
        public class UpdateInstruction
        {
            public string betId { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public enum PersistenceType
            {
                LAPSE=1,
                PERSIST,
                MARKET_ON_CLOSE
            }
            public PersistenceType newPersistenceType { get; set; }
        }
        public class ReplaceExecutionReport
        {
            public string customerRef { get; set; }
            public string marketId { get; set; }
            public ExecutionReportStatus status { get; set; }
            public ExecutionReportErrorCode? errorCode { get; set; }
            public class ReplaceInstructionReport
            {
                public InstructionReportStatus status { get; set; }
                public InstructionReportErrorCode? errorCode { get; set; }
                public CancelExecutionReport.CancelInstructionReport cancelInstructionReport { get; set; }
                public PlaceExecutionReport.PlaceInstructionReport placeInstructionReport { get; set; }
            }
            public ReplaceInstructionReport[] instructionReports { get; set; }
        }
        public class ReplaceInstruction
        {
            public string betId { get; set; }
            public double newPrice { get; set; }//KC
        }
        public class TimeRange
        {
            public DateTime from { get; set; }
            public DateTime to { get; set; }
        }
        public class CurrentOrderSummaryReport
        {
            public bool moreAvailable { get; set; }
            public class CurrentOrderSummary
            {
                public string betId { get; set; }
                public string marketId { get; set; }
                public long selectionId { get; set; }
                public double handicap { get; set; }
                public PriceSize priceSize { get; set; }
                public double bspLiability { get; set; }
                public Order.Side side { get; set; }
                public Order.OrderStatus status { get; set; }
                public Order.PersistenceType persistenceType { get; set; }
                public Order.OrderType orderType { get; set; }
                public DateTime placedDate { get; set; }
                public DateTime matchedDate { get; set; }
                public double? averagePriceMatched { get; set; }
                public double? sizeMatched { get; set; }
                public double? sizeRemaining { get; set; }
                public double? sizeLapsed { get; set; }
                public double? sizeCancelled { get; set; }
                public double? sizeVoided { get; set; }
                public string regulatorAuthCode { get; set; }
                public string regulatorCode { get; set; }
            }
            public CurrentOrderSummary[] currentOrders { get; set; }
        }
        public class RunnerId
        {
            public string marketId { get; set; }
            public long selectionId { get; set; }
            public double handicap { get; set; }
        }
        public class ClearedOrderSummaryReport
        {
            public bool moreAvailable { get; set; }
            public class ClearedOrderSummary
            {
                public string eventTypeId { get; set; }
                public string eventId { get; set; }
                public string marketId { get; set; }
                public long selectionId { get; set; }
                public double handicap { get; set; }
                public string betId { get; set; }
                public DateTime placedDate { get; set; }
                public Order.PersistenceType persistenceType { get; set; }
                public Order.OrderType orderType { get; set; }
                public Order.Side side { get; set; }
                public class ItemDescription
                {
                    public string eventTypeDesc { get; set; }
                    public string eventDesc { get; set; }
                    public string marketDesc { get; set; }
                    public DateTime marketStartTime { get; set; }
                    public string runnerDesc { get; set; }
                    public int numberOfWinners { get; set; }
                }
                public ItemDescription itemDescription { get; set; }
                public double priceRequested { get; set; }
                public DateTime settledDate { get; set; }
                public int betCount { get; set; }
                public double commission { get; set; }
                public double priceMatched { get; set; }
                public bool priceReduced { get; set; }
                public double sizeSettled { get; set; }
                public double profit { get; set; }
                public double sizeCancelled { get; set; }
            }
            public ClearedOrderSummary[] clearedOrders { get; set; }
        }
        public class MarketProfitAndLoss
        {
            public string marketId { get; set; }
            public double? commissionApplied { get; set; }
            public class RunnerProfitAndLoss
            {
                public long? selectionId { get; set; }
                public double? ifWin { get; set; }
                public double? ifLose { get; set; }
            }
            public RunnerProfitAndLoss[] profitAndLosses { get; set; }
        }
        public class _ContainerArray<T>
        {
            public T[] result{get;set;}
            public string jsonrpc {get; set; }
        }
        public class _Container<T>
        {
            public T result{get;set;}
            public string jsonrpc {get; set; }
        }
        public class LoginResp
        {
            public string sessionToken { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public Status loginStatus { get; set; }
            public enum Status 
            {
                SUCCESS,
                LIMITED_ACCESS,
                LOGIN_RESTRICTED,
                FAIL,
            }
        }
        #region Enums
        [JsonConverter(typeof(StringEnumConverter))]
        public enum TimeGranularity
        {
            DAYS = 1,
            HOURS,
            MINUTES
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ExecutionReportStatus
        {
            SUCCESS = 1,
            FAILURE,
            PROCESSED_WITH_ERRORS,
            TIMEOUT
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum ExecutionReportErrorCode
        {
            ERROR_IN_MATCHER = 1,
            PROCESSED_WITH_ERRORS,
            BET_ACTION_ERROR,
            INVALID_ACCOUNT_STATE,
            INVALID_WALLET_STATUS,
            INSUFFICIENT_FUNDS,
            LOSS_LIMIT_EXCEEDED,
            MARKET_SUSPENDED,
            MARKET_NOT_OPEN_FOR_BETTING,
            DUPLICATE_TRANSACTION,
            INVALID_ORDER,
            INVALID_MARKET_ID,
            PERMISSION_DENIED,
            DUPLICATE_BETIDS,
            NO_ACTION_REQUIRED,
            SERVICE_UNAVAILABLE,
            REJECTED_BY_REGULATOR
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum InstructionReportStatus
        {
            SUCCESS = 1,
            FAILURE,
            TIMEOUT
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum InstructionReportErrorCode
        {
            INVALID_BET_SIZE = 1,
            INVALID_RUNNER,
            BET_TAKEN_OR_LAPSED,
            BET_IN_PROGRESS,
            RUNNER_REMOVED,
            MARKET_NOT_OPEN_FOR_BETTING,
            LOSS_LIMIT_EXCEEDED,
            MARKET_NOT_OPEN_FOR_BSP_BETTING,
            INVALID_PRICE_EDIT,
            INVALID_ODDS,
            INSUFFICIENT_FUNDS,
            INVALID_PERSISTENCE_TYPE,
            ERROR_IN_MATCHER,
            INVALID_BACK_LAY_COMBINATION,
            ERROR_IN_ORDER,
            INVALID_BID_TYPE,
            INVALID_BET_ID,
            CANCELLED_NOT_PLACED,
            RELATED_ACTION_FAILED,
            NO_ACTION_REQUIRED
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum OrderBy
        {
            BY_BET = 1,
            BY_MARKET,
            BY_MATCH_TIME,
            BY_PLACE_TIME,
            BY_SETTLED_TIME,
            BY_VOID_TIME
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum SortDir
        {
            EARLIEST_TO_LATEST = 1,
            LATEST_TO_EARLIEST
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum BetStatus
        {
            SETTLED = 1,
            VOIDED,
            LAPSED,
            CANCELLED
        }

        [JsonConverter(typeof(StringEnumConverter))]
        public enum GroupBy
        {
            EVENT_TYPE = 1,
            EVENT,
            MARKET,
            SIDE,
            BET
        }
        #endregion
        #endregion
        #region Filtrs
        public static IDictionary<string,object> EmptyFiltr { get { return MarketFiltr.GetFiltr(); } }
        public class MarketFiltr
        {
            #region Market Filtr
            private IDictionary<string, object> FiltrList;
            [JsonConverter(typeof(StringEnumConverter))]
            public enum FiltrNames
            {
                EMPTY=0,
                textQuery,//filtracvum e @st nshvac teqsti, * - i demqum teqsti skbzi het ham@nknum
                exchangeIds,
                eventTypeIds,//filtracvum e @st nshvac sporteri ID neri
                eventIds,//filtracvum e @st nshvac xaxeri ID neri 
                competitionIds,//filtracvum e @st nshvac chempionatneri ID neri
                marketIds,//filtracvum e @st nshvac marketneri ID neri
                venues,//filtracnum e @st nshvac vayri(vortex kayanalu e xax@), diavazqi hamar
                bspOnly,//True-i depqum miayn BSP marketner, Fals-i depqum bolor@ baci BSP-neric,chnshelu depqum erkus@
                turnInPlayEnabled,//market@ onlay bace linelu(true) te voch(false),chnshelu depqum bolor@
                inPlayOnly,//marketner@ voronq hima gtnvum en online(true) ka voch(false),chnshelu depqum erkus@
                marketBettingTypes,
                marketCountries,
                marketTypeCodes,
                marketStartTime,
                withOrders
            }
            public MarketFiltr()
            {
                FiltrList = new Dictionary<string, object>();
                FiltrList["filter"] = new Dictionary<string,object>();
            }
            private static IDictionary<string, object> DefaultValueOfFiltr(FiltrNames FN)
            {
                IDictionary<string, object> temp = new Dictionary<string, object>();
                switch (FN)
                {
                    case FiltrNames.textQuery: temp["textQuery"] = new { }; break;
                    case FiltrNames.exchangeIds: temp["exchangeIds"] = new List<string>(); break;
                    case FiltrNames.eventTypeIds: temp["eventTypeIds"] = new List<string>(); break;
                    case FiltrNames.eventIds: temp["eventIds"] = new List<string>(); break;
                    case FiltrNames.competitionIds: temp["competitionIds"] = new List<string>(); break;
                    case FiltrNames.marketIds: temp["marketIds"] = new List<string>(); break;
                    case FiltrNames.venues: temp["venues"] = new List<string>(); break;
                    case FiltrNames.bspOnly: temp["bspOnly"] = new { }; break;
                    case FiltrNames.turnInPlayEnabled: temp["turnInPlayEnabled"] = new { }; break;
                    case FiltrNames.inPlayOnly: temp["inPlayOnly"] = new { }; break;
                    case FiltrNames.marketBettingTypes: temp["marketBettingTypes"] = new List<object>(); break;
                    case FiltrNames.marketCountries: temp["marketCountries"] = new List<string>(); break;
                    case FiltrNames.marketTypeCodes: temp["marketTypeCodes"] = new List<string>(); break;
                    case FiltrNames.marketStartTime: temp["marketStartTime"] = new { }; break;
                    case FiltrNames.withOrders: temp["withOrders"] = new List<object>(); break;
                }
                return temp;
            }
            public static IDictionary<string, object> GetFiltr(FiltrNames FN=0, object ob = null)
            {
                IDictionary<string, object> temp = new Dictionary<string, object>();
                if ((int)FN == 0) 
                { 
                    temp["filter"] = new object();
                    return temp; 
                }
                if (ob == null)
                {
                    temp["filter"] = DefaultValueOfFiltr(FN);
                    return  temp;
                }
                IDictionary<string, object> temp2 = new Dictionary<string, object>();
                temp2[FN.ToString()] = ob;
                temp["filter"] = temp2;
                return temp;
            }
            public MarketFiltr AddFiltr(FiltrNames FN, object ob = null)
            {
                if (ob == null)
                {
                    ((IDictionary<string,object>)(FiltrList["filter"]))[FN.ToString()] = DefaultValueOfFiltr(FN);
                    return this;
                }
                ((IDictionary<string,object>)FiltrList["filter"])[FN.ToString()] = ob;
                return this;
            }
            public IDictionary<string, object> Filtr { get { return this.FiltrList; } }
            public void clear(FiltrNames? FL = null)
            {
                if (FL == null) FiltrList.Clear();
                else FiltrList.Remove(FL.ToString());
            }
            #endregion
            #region MarketProjection
            public class MarketProjection
            {
                private List<string> mp;
                private MarketProjection() { mp = new List<string>(); }
                public MarketProjection(string MP)
                {
                    mp = new List<string>();
                    mp.Add(MP);
                }
                public string[] MP { get { if (mp == null)return null; return mp.ToArray(); } }
                public int getMaxResult()
                {
                    int temp = 0;
                    foreach (var el in mp) if (el == "MARKET_DESCRIPTION" || el == "RUNNER_METADATA") temp++;
                    if (temp == 0) return 1000;
                    else if (temp == 1 || temp == 2) return 200 / temp;
                    return 0;
                }
                public static MarketProjection selectAll()
                {
                    MarketProjection temp = new MarketProjection();
                    temp.mp.AddRange(new string[] {
                        "COMPETITION",
                        "EVENT",
                        "EVENT_TYPE",
                        "MARKET_START_TIME",
                        "MARKET_DESCRIPTION",
                        "RUNNER_METADATA" });
                    return temp;
                }
                public static MarketProjection COMPETITION { get { return new MarketProjection("COMPETITION"); } }
                public static MarketProjection EVENT { get { return new MarketProjection("EVENT"); } }
                public static MarketProjection EVENT_TYPE { get { return new MarketProjection("EVENT_TYPE"); } }
                public static MarketProjection MARKET_START_TIME { get { return new MarketProjection("MARKET_START_TIME"); } }
                public static MarketProjection MARKET_DESCRIPTION { get { return new MarketProjection("MARKET_DESCRIPTION"); } }
                public static MarketProjection RUNNER_DESCRIPTION { get { return new MarketProjection("RUNNER_DESCRIPTION"); } }
                public static MarketProjection RUNNER_METADATA { get { return new MarketProjection("RUNNER_METADATA"); } }
                public static MarketProjection operator +(MarketProjection s1, MarketProjection s2)
                {
                    if (s1 == null) return s2;
                    if (s2 == null) return s1;
                    foreach (var el in s2.mp) if (s1.mp.IndexOf(el) == -1) s1.mp.Add(el);
                    return s1;
                }

            }
            #endregion
            #region MarketSort
            public enum MarketSort
            {
                MINIMUM_TRADED,
                MAXIMUM_TRADED,
                MINIMUM_AVAILABLE,
                MAXIMUM_AVAILABLE,
                FIRST_TO_START,
                LAST_TO_START
            }
            #endregion
        }
        public class PriceProjection
        {
            public bool virtualise { get; set; }
            public bool rolloverStakes { get; set; }
            public class ExBestOffersOverrides
            {
                public int bestPricesDepth { get; set; }
                public int rollupLimit { get; set; }
                public double rollupLiabilityThreshold { get; set; }
                public int rollupLiabilityFactor { get; set; }
                public enum RollupModel
                {
                    STAKE,
                    PAYOUT,
                    MANAGED_LIABILITY,
                    NONE
                }
                public RollupModel rollupModel { get; set; }
            }
            public ExBestOffersOverrides exBestOffersOverrides { get; set; }
            public class PriceData
            {
                private List<string> _PD;
                private PriceData() { _PD = new List<string>(); }
                private PriceData(string pd): this()
                {
                    _PD.Add(pd);
                }
                public string[] PD { get { return _PD.ToArray(); } }
                public static PriceData SP_AVAILABLE { get { return new PriceData("SP_AVAILABLE"); } }
                public static PriceData SP_TRADED { get { return new PriceData("SP_TRADED"); } }
                public static PriceData EX_BEST_OFFERS { get { return new PriceData("EX_BEST_OFFERS"); } }
                public static PriceData EX_ALL_OFFERS { get { return new PriceData("EX_ALL_OFFERS"); } }
                public static PriceData EX_TRADED { get { return new PriceData("EX_TRADED"); } }
                public static PriceData operator+(PriceData pd1,PriceData pd2)
                {
                    if (pd1 == null) return pd2;
                    if (pd2 == null) return pd1;
                    foreach (var el in pd2._PD) if (pd1._PD.IndexOf(el) == -1) pd1._PD.Add(el);
                    return pd1;
                }
                public static PriceData selectAll()
                {
                    PriceData temp = new PriceData();
                    temp._PD.AddRange(new string[] { 
                        "SP_AVAILABLE",
                        "SP_TRADED",
                        "EX_BEST_OFFERS",
                        "EX_ALL_OFFERS",
                        "EX_TRADED" });
                    return temp;
                }
            }
            public void SetPriceData(PriceData pd)
            {
                prDa = pd;
            }
            public int getMaxMarketIdsCount()
            {
                if (prDa == null) return MaxPoints / 2;
                int I = 0;
                foreach (var el in prDa.PD)
                {
                    switch (el)
                    {
                        case "SP_AVAILABLE": I += 3; break;
                        case "SP_TRADED": I += 7; break;
                        case "EX_BEST_OFFERS": I += 5; break;
                        case "EX_ALL_OFFERS": I += 17; break;
                        case "EX_TRADED": I += 17; break;
                    }
                }
                if (I == 0) throw new ArgumentException("PriceData {prDa} have incorrect valu", "getMaxMarketIdsCount()");
                return MaxPoints / I + (MaxPoints % I == 0 ? 0 : 1);
            }
            private PriceData prDa;
            public string[] priceData { get { if (prDa == null)return null; return prDa.PD; } }
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public enum OrderProjection
        {
            ALL,
            EXECUTABLE,
            EXECUTION_COMPLETE
        }
        [JsonConverter(typeof(StringEnumConverter))]
        public enum MatchProjection
        {
            NO_ROLLUP,
            ROLLED_UP_BY_PRICE,
            ROLLED_UP_BY_AVG_PRICE
        }

        
        #endregion
        #region Request values
        private bool loginStatus = false;
        public bool LoginStatus { get { return loginStatus; } }
        private string LocaleCode = "en";
        public const int MaxPoints = 200;
        public string AppKey;
        private string Token;
        public string Certificat_P12;
        public string Certificat_ExportKey;
        private Uri EndPoint = new Uri("https://api.betfair.com/exchange/betting/json-rpc/v1");
        #endregion
        #region Response values
        #endregion
        #region Functions
            #region nextKc
        public static double nextKcBack(double kc)
        {
            kc = Math.Round(kc, 2);
            if (kc >= 1 && kc < 2) return kc + 0.01;
            else if (kc >= 2 && kc < 3) return kc + 0.02;
            else if (kc >= 3 && kc < 4) return kc + 0.05;
            else if (kc >= 4 && kc < 6) return kc + 0.1;
            else if (kc >= 6 && kc < 10) return kc + 0.2;
            else if (kc >= 10 && kc < 20) return kc + 0.5;
            else if (kc >= 20 && kc < 30) return kc + 1;
            else if (kc >= 30 && kc < 50) return kc + 2;
            else if (kc >= 50 && kc < 100) return kc + 5;
            else if (kc >= 100 && kc < 1000) return kc + 10;
            else if (kc < 1) return 1.01;
            return 1000;
        }
        public static double nextAsianKcBack(double kc)
        {
            kc = Math.Round(kc, 2);
            if (kc >= 1 && kc < 1000) return kc + 0.01;
            if (kc < 1) return 1.01;
            return 1000;
        }
        public static double nextKcLay(double kc)
        {
            kc = Math.Round(kc, 2);
            if (kc > 1 && kc <= 2) return kc - 0.01;
            else if (kc > 2 && kc <= 3) return kc - 0.02;
            else if (kc > 3 && kc <= 4) return kc - 0.05;
            else if (kc > 4 && kc <= 6) return kc - 0.1;
            else if (kc > 6 && kc <= 10) return kc - 0.2;
            else if (kc > 10 && kc <= 20) return kc - 0.5;
            else if (kc > 20 && kc <= 30) return kc - 1;
            else if (kc > 30 && kc <= 50) return kc - 2;
            else if (kc > 50 && kc <= 100) return kc - 5;
            else if (kc > 100 && kc <= 1000) return kc - 10;
            else if (kc > 1000) return 1000;
            return 1.01;
        }
        public static double nextAsianKcLay(double kc)
        {
            kc = Math.Round(kc, 2);
            if (kc > 1 && kc <= 1000) return kc - 0.01;
            else if (kc > 1000) return 1000;
            return 1.01;
        }
        #endregion
            #region CodToCountry
            public static string CodToCountry(string CountryCod)
            {
                #region CountryCod & CountryName
                string[,] ptr = new string[,]{
                    {"AF","Afghanistan"},
                    {"AX","Aland Islands !Åland Islands"},
                    {"AL","Albania"},
                    {"DZ","Algeria"},
                    {"AS","American Samoa"},
                    {"AD","Andorra"},
                    {"AO","Angola"},
                    {"AI","Anguilla"},
                    {"AQ","Antarctica"},
                    {"AG","Antigua and Barbuda"},
                    {"AR","Argentina"},
                    {"AM","Armenia"},
                    {"AW","Aruba"},
                    {"AU","Australia"},
                    {"AT","Austria"},
                    {"AZ","Azerbaijan"},
                    {"BS","Bahamas"},
                    {"BH","Bahrain"},
                    {"BD","Bangladesh"},
                    {"BB","Barbados"},
                    {"BY","Belarus"},
                    {"BE","Belgium"},
                    {"BZ","Belize"},
                    {"BJ","Benin"},
                    {"BM","Bermuda"},
                    {"BT","Bhutan"},
                    {"BO","Bolivia, Plurinational State of"},
                    {"BQ","Bonaire, Sint Eustatius and Saba"},
                    {"BA","Bosnia and Herzegovina"},
                    {"BW","Botswana"},
                    {"BV","Bouvet Island"},
                    {"BR","Brazil"},
                    {"IO","British Indian Ocean Territory"},
                    {"BN","Brunei Darussalam"},
                    {"BG","Bulgaria"},
                    {"BF","Burkina Faso"},
                    {"BI","Burundi"},
                    {"CV","Cabo Verde"},
                    {"KH","Cambodia"},
                    {"CM","Cameroon"},
                    {"CA","Canada"},
                    {"KY","Cayman Islands"},
                    {"CF","Central African Republic"},
                    {"TD","Chad"},
                    {"CL","Chile"},
                    {"CN","China"},
                    {"CX","Christmas Island"},
                    {"CC","Cocos (Keeling) Islands"},
                    {"CO","Colombia"},
                    {"KM","Comoros"},
                    {"CG","Congo"},
                    {"CD","Congo, the Democratic Republic of the"},
                    {"CK","Cook Islands"},
                    {"CR","Costa Rica"},
                    {"CI","Cote d'Ivoire !Côte d'Ivoire"},
                    {"HR","Croatia"},
                    {"CU","Cuba"},
                    {"CW","Curaçao"},
                    {"CY","Cyprus"},
                    {"CZ","Czech Republic"},
                    {"DK","Denmark"},
                    {"DJ","Djibouti"},
                    {"DM","Dominica"},
                    {"DO","Dominican Republic"},
                    {"EC","Ecuador"},
                    {"EG","Egypt"},
                    {"SV","El Salvador"},
                    {"GQ","Equatorial Guinea"},
                    {"ER","Eritrea"},
                    {"EE","Estonia"},
                    {"ET","Ethiopia"},
                    {"FK","Falkland Islands (Malvinas)"},
                    {"FO","Faroe Islands"},
                    {"FJ","Fiji"},
                    {"FI","Finland"},
                    {"FR","France"},
                    {"GF","French Guiana"},
                    {"PF","French Polynesia"},
                    {"TF","French Southern Territories"},
                    {"GA","Gabon"},
                    {"GM","Gambia"},
                    {"GE","Georgia"},
                    {"DE","Germany"},
                    {"GH","Ghana"},
                    {"GI","Gibraltar"},
                    {"GR","Greece"},
                    {"GL","Greenland"},
                    {"GD","Grenada"},
                    {"GP","Guadeloupe"},
                    {"GU","Guam"},
                    {"GT","Guatemala"},
                    {"GG","Guernsey"},
                    {"GN","Guinea"},
                    {"GW","Guinea-Bissau"},
                    {"GY","Guyana"},
                    {"HT","Haiti"},
                    {"HM","Heard Island and McDonald Islands"},
                    {"VA","Holy See (Vatican City State)"},
                    {"HN","Honduras"},
                    {"HK","Hong Kong"},
                    {"HU","Hungary"},
                    {"IS","Iceland"},
                    {"IN","India"},
                    {"ID","Indonesia"},
                    {"IR","Iran, Islamic Republic of"},
                    {"IQ","Iraq"},
                    {"IE","Ireland"},
                    {"IM","Isle of Man"},
                    {"IL","Israel"},
                    {"IT","Italy"},
                    {"JM","Jamaica"},
                    {"JP","Japan"},
                    {"JE","Jersey"},
                    {"JO","Jordan"},
                    {"KZ","Kazakhstan"},
                    {"KE","Kenya"},
                    {"KI","Kiribati"},
                    {"KP","Korea, Democratic People's Republic of"},
                    {"KR","Korea, Republic of"},
                    {"KW","Kuwait"},
                    {"KG","Kyrgyzstan"},
                    {"LA","Lao People's Democratic Republic"},
                    {"LV","Latvia"},
                    {"LB","Lebanon"},
                    {"LS","Lesotho"},
                    {"LR","Liberia"},
                    {"LY","Libya"},
                    {"LI","Liechtenstein"},
                    {"LT","Lithuania"},
                    {"LU","Luxembourg"},
                    {"MO","Macao"},
                    {"MK","Macedonia, the former Yugoslav Republic of"},
                    {"MG","Madagascar"},
                    {"MW","Malawi"},
                    {"MY","Malaysia"},
                    {"MV","Maldives"},
                    {"ML","Mali"},
                    {"MT","Malta"},
                    {"MH","Marshall Islands"},
                    {"MQ","Martinique"},
                    {"MR","Mauritania"},
                    {"MU","Mauritius"},
                    {"YT","Mayotte"},
                    {"MX","Mexico"},
                    {"FM","Micronesia, Federated States of"},
                    {"MD","Moldova, Republic of"},
                    {"MC","Monaco"},
                    {"MN","Mongolia"},
                    {"ME","Montenegro"},
                    {"MS","Montserrat"},
                    {"MA","Morocco"},
                    {"MZ","Mozambique"},
                    {"MM","Myanmar"},
                    {"NA","Namibia"},
                    {"NR","Nauru"},
                    {"NP","Nepal"},
                    {"NL","Netherlands"},
                    {"NC","New Caledonia"},
                    {"NZ","New Zealand"},
                    {"NI","Nicaragua"},
                    {"NE","Niger"},
                    {"NG","Nigeria"},
                    {"NU","Niue"},
                    {"NF","Norfolk Island"},
                    {"MP","Northern Mariana Islands"},
                    {"NO","Norway"},
                    {"OM","Oman"},
                    {"PK","Pakistan"},
                    {"PW","Palau"},
                    {"PS","Palestine, State of"},
                    {"PA","Panama"},
                    {"PG","Papua New Guinea"},
                    {"PY","Paraguay"},
                    {"PE","Peru"},
                    {"PH","Philippines"},
                    {"PN","Pitcairn"},
                    {"PL","Poland"},
                    {"PT","Portugal"},
                    {"PR","Puerto Rico"},
                    {"QA","Qatar"},
                    {"RE","Reunion !Réunion"},
                    {"RO","Romania"},
                    {"RU","Russian Federation"},
                    {"RW","Rwanda"},
                    {"BL","Saint Barthélemy"},
                    {"SH","Saint Helena, Ascension and Tristan da Cunha"},
                    {"KN","Saint Kitts and Nevis"},
                    {"LC","Saint Lucia"},
                    {"MF","Saint Martin (French part)"},
                    {"PM","Saint Pierre and Miquelon"},
                    {"VC","Saint Vincent and the Grenadines"},
                    {"WS","Samoa"},
                    {"SM","San Marino"},
                    {"ST","Sao Tome and Principe"},
                    {"SA","Saudi Arabia"},
                    {"SN","Senegal"},
                    {"RS","Serbia"},
                    {"SC","Seychelles"},
                    {"SL","Sierra Leone"},
                    {"SG","Singapore"},
                    {"SX","Sint Maarten (Dutch part)"},
                    {"SK","Slovakia"},
                    {"SI","Slovenia"},
                    {"SB","Solomon Islands"},
                    {"SO","Somalia"},
                    {"ZA","South Africa"},
                    {"GS","South Georgia and the South Sandwich Islands"},
                    {"SS","South Sudan"},
                    {"ES","Spain"},
                    {"LK","Sri Lanka"},
                    {"SD","Sudan"},
                    {"SR","Suriname"},
                    {"SJ","Svalbard and Jan Mayen"},
                    {"SZ","Swaziland"},
                    {"SE","Sweden"},
                    {"CH","Switzerland"},
                    {"SY","Syrian Arab Republic"},
                    {"TW","Taiwan, Province of China"},
                    {"TJ","Tajikistan"},
                    {"TZ","Tanzania, United Republic of"},
                    {"TH","Thailand"},
                    {"TL","Timor-Leste"},
                    {"TG","Togo"},
                    {"TK","Tokelau"},
                    {"TO","Tonga"},
                    {"TT","Trinidad and Tobago"},
                    {"TN","Tunisia"},
                    {"TR","Turkey"},
                    {"TM","Turkmenistan"},
                    {"TC","Turks and Caicos Islands"},
                    {"TV","Tuvalu"},
                    {"UG","Uganda"},
                    {"UA","Ukraine"},
                    {"AE","United Arab Emirates"},
                    {"GB","United Kingdom"},
                    {"US","United States"},
                    {"UM","United States Minor Outlying Islands"},
                    {"UY","Uruguay"},
                    {"UZ","Uzbekistan"},
                    {"VU","Vanuatu"},
                    {"VE","Venezuela, Bolivarian Republic of"},
                    {"VN","Viet Nam"},
                    {"VG","Virgin Islands, British"},
                    {"VI","Virgin Islands, U.S."},
                    {"WF","Wallis and Futuna"},
                    {"EH","Western Sahara"},
                    {"YE","Yemen"},
                    {"ZM","Zambia"},
                    {"ZW","Zimbabwe"}
                };
                #endregion
                for (int i = 0; i < ptr.Length/2; i++)
                {
                    if (CountryCod == ptr[i, 0]) return ptr[i, 1];
                }
                return CountryCod;
            }
            #endregion
            #region getRequest
            private HttpWebRequest getRequest(Uri httpUri)
            {
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(httpUri);
                request.Method = "POST";
                request.ContentType = "application/json-rpc";
                request.ContentLength = 0;
                request.Headers.Add(HttpRequestHeader.AcceptCharset, "ISO-8859-1,utf-8");
                request.Headers.Add("X-Authentication", Token);
                request.Headers.Add("X-Application", AppKey);
                request.Accept = "application/json";
                return request;
            }
            #endregion
            #region Invoke<T>
            public async Task<string> AInvoke<T>(string method, IDictionary<string, object> args)
            {
                try
                {
                    #region Method not NULL
                    if (method == null) throw new ArgumentException("method");
                    if (method.Length == 0) throw new ArgumentException(null, "method");
                    #endregion
                    #region Json Serilize to string
                    IDictionary<string, object> o = new Dictionary<string, object>();
                    args["locale"] = LocaleCode;
                    o["jsonrpc"] = "2.0";
                    o["method"] = "SportsAPING/v1.0/" + method;
                    o["params"] = args;
                    o["id"] = 1;
                    JsonSerializerSettings JsonSetings = new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore };
                    string DataJsonString = JsonConvert.SerializeObject(o, JsonSetings);
                    #endregion
                    #region Get HttpWebRequest
                    HttpWebRequest request = getRequest(EndPoint);
                    ////////////////////DATA/////////////////////////////////////
                    byte[] byteArray = Encoding.UTF8.GetBytes(DataJsonString);
                    request.ContentLength = byteArray.Length;
                    Stream reqStream = request.GetRequestStream();
                    reqStream.Write(byteArray, 0, byteArray.Length);
                    reqStream.Close();
                    /////////////////////////////////////////////////////////////
                    #endregion
                    string respString=null;
                    for (int i = 0; ; i++) 
                    {
                        try
                        {
                            if (i == 2)
                            {
                                await this.Logout();
                                await this.Login(IExchange.Properties.Settings.Default.Username, IExchange.Properties.Settings.Default.Password);
                            }
                            using (HttpWebResponse resp = (HttpWebResponse)await request.GetResponseAsync())
                            using (StreamReader sreaad = new StreamReader(resp.GetResponseStream()))
                            {
                                respString = await sreaad.ReadToEndAsync();
                            }
                        }
                        catch (System.Net.WebException WE)
                        {
                            if (i == 5) return null;
                           
                            if (WE.Status == WebExceptionStatus.Timeout) continue;
                            return null;
                        }
                        break;
                    }
                    return respString;
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "C[Betfair]F[AInvoke<T>()]");
                    return null;
                }
            }
            public async Task<T[]> InvokeArr<T>(string method, IDictionary<string, object> args)
            {
                string respString=await AInvoke<T>(method,args);
                return JsonConvert.DeserializeObject<_ContainerArray<T>>(respString).result;
            }
            public async Task<T> Invoke<T>(string method, IDictionary<string, object> args)
            {
                string respString=await AInvoke<T>(method,args);
                return JsonConvert.DeserializeObject<_Container<T>>(respString).result;;
            }
            #endregion
            #region Login
            public async Task<string> Login(string username, string password)
            {
                Uri LoginEndPoint = new Uri("https://identitysso.betfair.com/api/certlogin");
                HttpWebRequest request = (HttpWebRequest)WebRequest.Create(LoginEndPoint);
                request.Method = "POST";
                /////////////////////////////////////////////////////////////
                X509Certificate2 myCertificat = new X509Certificate2(Certificat_P12, Certificat_ExportKey);
                request.ClientCertificates.Add(myCertificat);
                request.Headers.Add("X-Application", AppKey);
                request.ContentType = "application/x-www-form-urlencoded";
                /////////////////////////////////////////////////////////////
                string PostData = "username=" + username + "&password=" + password;
                byte[] byteArray = Encoding.UTF8.GetBytes(PostData);
                request.ContentLength = byteArray.Length;
                Stream dataStream = request.GetRequestStream();
                dataStream.Write(byteArray, 0, byteArray.Length);
                dataStream.Close();
                /////////////////////////////////////////////////////////////
                WebResponse  resp=await request.GetResponseAsync();
                string reader =await new StreamReader( resp.GetResponseStream()).ReadToEndAsync();
                LoginResp login = JsonConvert.DeserializeObject<LoginResp>(reader);
                Token = login.sessionToken;
                string status = login.loginStatus.ToString();
                if (status == "SUCCESS") loginStatus = true;
                ///////////////////////////////////////////////////////////////
                System.Timers.Timer KeepAliveTimer = new System.Timers.Timer(15 * 60 * 1000);
                KeepAliveTimer.Elapsed += async (a, b) => { await this.KeepAlive(); };
                KeepAliveTimer.Start();
                return "\nLogin Status: " + status;
            }
            #endregion
            #region Logout
            public async Task<string> Logout()
            {
                loginStatus = false;
                Uri LogoutEndPoint =new Uri("https://identitysso.betfair.com/api/logout");
                HttpWebRequest request = getRequest(LogoutEndPoint);
                WebResponse response =await request.GetResponseAsync();
                string reader =await new StreamReader(response.GetResponseStream()).ReadToEndAsync();
                dynamic logout = JsonConvert.DeserializeObject<dynamic>(reader);
                string status = logout.status;
                if (status != "SUCCESS") status += "\nError: " + logout.error;
                return "\nLog Out Status: "+status;
            }
            #endregion
            #region KeepAlive
            private async Task KeepAlive()
            {
                Uri keepaliveEndPoint = new Uri("https://identitysso.betfair.com/api/keepAlive");
                HttpWebRequest request = getRequest(keepaliveEndPoint);
                string reader =await new StreamReader((await request.GetResponseAsync()).GetResponseStream()).ReadToEndAsync();
                dynamic keepAlive = JsonConvert.DeserializeObject<dynamic>(reader);
            }
            #endregion
            #region listTimeRanges
            public async Task<TimeRangeResult[]> listTimeRanges(IDictionary<string,object> filter, TimeGranularity granularity)
            {
                if (filter == null) return null;
                filter["granularity"]=granularity;
                return await InvokeArr<TimeRangeResult>("listTimeRanges", filter);
            }
            #endregion
            #region listVenues
            public async Task<VenueResult[]> listVenues(IDictionary<string, object> filtr)
            {
                if (filtr == null) return null;
                return await InvokeArr<VenueResult>("listVenues", filtr);
            }
            #endregion
            #region getEventTyps
            public async Task<EventTypeResult[]> getEventTyps(IDictionary<string, object> filtr)//All sports
            {
                return await InvokeArr<EventTypeResult>("listEventTypes", filtr);
            }
            #endregion
            #region getEvens
            public async Task<EventResult[]> getEvens(IDictionary<string, object> filtr)
            {
                return await InvokeArr<EventResult>("listEvents", filtr);
            }//All Games
            #endregion
            #region getCountries
            public async Task<CountryCodeResult[]> getCountries(IDictionary<string, object> filtr)//All countries
            {
                return await InvokeArr<CountryCodeResult>("listCountries", filtr);
            }
            #endregion
            #region getCompetitions
            public async  Task<CompetitionResult[]> getCompetitions(IDictionary<string, object> filtr)//All championships
            {
                return await InvokeArr<CompetitionResult>("listCompetitions", filtr);
            }
            #endregion
            #region getMarketTypes
            public async Task<MarketTypeResult[]> getMarketTypes(IDictionary<string, object> filtr)
            {
                return await InvokeArr<MarketTypeResult>("listMarketTypes", filtr);
            }
            #endregion
            #region getMarketCatalogue
            public async Task<MarketCatalogue[]> getMarketCatalogue(IDictionary<string, object> filtr, int MaxResults = 1,MarketFiltr.MarketProjection MarPr=null,MarketFiltr.MarketSort? sort=null)
            {
                if (MaxResults < 0 || MaxResults > 1000) throw new ArgumentException("MaxResults", MaxResults.ToString());
                filtr["maxResults"] = MaxResults;
                if (MarPr != null && MarPr.MP != null) filtr["marketProjection"] = MarPr.MP;
                if (sort != null) filtr["sort"] = sort;
                return await InvokeArr<MarketCatalogue>("listMarketCatalogue", filtr);
            }
            #endregion
            #region listMarketBook
            static ThreadOptimization<string, MarketBook> listMarketBookOptimization = null;
            public async Task<MarketBook[]> OlistMarketBook(string[] marketIds, PriceProjection pP = null, OrderProjection? oP = null, MatchProjection? mP = null)
            {
                if (listMarketBookOptimization == null) listMarketBookOptimization = new ThreadOptimization<string, MarketBook>(_ => listMarketBook(_, pP, oP, mP), _ => _.Length * 5, null, 200);
                var el =  listMarketBookOptimization.Optimization(marketIds);
                await el.WaitingResp();
                return el.Resp;
            }
            public async Task<MarketBook[]> listMarketBook(string[] marketIds, PriceProjection pP = null, OrderProjection? oP = null, MatchProjection? mP = null)
            {
                
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                if (marketIds == null || marketIds.Any(_ => _ == null)) return null;
                Filtr["marketIds"] = marketIds;
                if (pP != null) Filtr["priceProjection"] = pP;
                if (oP != null) Filtr["orderProjection"] = oP;
                if (mP != null) Filtr["matchProjection"] = mP;
                List<MarketBook> ptr =(await InvokeArr<MarketBook>("listMarketBook", Filtr)).ToList();
                MarketBook[] resp = new MarketBook[marketIds.Length];
                for (int i = 0; i < marketIds.Length; i++)
                {
                    foreach (var el in ptr)
                    {
                        if (el.marketId == marketIds[i])
                        {
                            resp[i] = el;
                            //ptr.Remove(el);
                            break;
                        }
                    }
                }
                return resp;
                //return await InvokeArr<MarketBook>("listMarketBook", Filtr);
            }
            #endregion
            #region placeOrders
            public async Task<PlaceExecutionReport> placeOrders(string marketId, List<PlaceInstruction> instructions, string customerRef=null)
            {
                if (marketId == null || marketId == "") return null;
                if (instructions == null) return null;
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["marketId"] = marketId;
                Filtr["instructions"] = instructions.ToArray();
                if(customerRef!=null) Filtr["customerRef"] = customerRef;
                return await Invoke<PlaceExecutionReport>("placeOrders", Filtr);
            }
        #endregion
            #region cancelOrders
            public async Task<CancelExecutionReport> cancelOrders(string marketId, CancelInstruction[] instructions, string customerRef = null)
            {
                if (marketId == null || marketId == "" || instructions==null) return null;
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["marketId"] = marketId;
                Filtr["instructions"] = instructions;
                if (customerRef != null && customerRef != "") Filtr["customerRef"] = customerRef;
                return await Invoke<CancelExecutionReport>("cancelOrders", Filtr);
            }
            #endregion
            #region updateOrders
            public async Task<UpdateExecutionReport> updateOrders(string marketId, UpdateInstruction[] instructions, string customerRef=null)
            {
                if (marketId == null || marketId == "" || instructions==null) return null;
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["marketId"] = marketId;
                Filtr["instructions"] = instructions;
                if (customerRef != null && customerRef != "") Filtr["customerRef"] = customerRef;
                return await Invoke<UpdateExecutionReport>("updateOrders", Filtr);
            }
            #endregion
            #region replaceOrders
            public async Task<ReplaceExecutionReport> replaceOrders(string marketId, ReplaceInstruction[] instructions, string customerRef = null)
            {
                if (marketId == null || marketId == "" || instructions == null) return null;
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["marketId"] = marketId;
                Filtr["instructions"] = instructions;
                if (customerRef != null && customerRef != "") Filtr["customerRef"] = customerRef;
                return await Invoke<ReplaceExecutionReport>("replaceOrders", Filtr);
            }
            #endregion
            #region listCurrentOrders 
            #region OPTIMIZATION
            static ThreadOptimization<string, CurrentOrderSummaryReport.CurrentOrderSummary> listCurrentOrdersOptimization = null;
            public async Task<CurrentOrderSummaryReport.CurrentOrderSummary[]> listCurrentOrdersBI(string[] betIds)
            {
                var res = await listCurrentOrders(betIds);
                CurrentOrderSummaryReport.CurrentOrderSummary[] result = new CurrentOrderSummaryReport.CurrentOrderSummary[betIds.Length];
                for (int i = 0; i < betIds.Length; i++)
                {
                    foreach (var el in res.currentOrders)
                    {
                        if (el.betId == betIds[i])
                        {
                            result[i] = el;
                            break;
                        }
                    }
                }
                return result;
            }
            public async Task<CurrentOrderSummaryReport.CurrentOrderSummary[]> OlistCurrentOrders(string[] BetIds)
            {
                if (listCurrentOrdersOptimization == null) listCurrentOrdersOptimization = new ThreadOptimization<string, CurrentOrderSummaryReport.CurrentOrderSummary>(listCurrentOrdersBI, _ => _.Length, null, 1000);
                var resp = listCurrentOrdersOptimization.Optimization(BetIds);
                await resp.WaitingResp();
                return resp.Resp;
            }
            #endregion
            public async Task<CurrentOrderSummaryReport> listCurrentOrders(string[] betIds = null, string[] marketIds = null, OrderProjection? orderProjection = null, TimeRange dateRange = null, OrderBy? orderBy = null, SortDir? sortDir = null, int fromRecord = 0, int recordCount = 0)
            {
                try
                {
                    IDictionary<string, object> Filtr = new Dictionary<string, object>();
                    if (betIds != null) Filtr["betIds"] = betIds;
                    if (marketIds != null) Filtr["marketIds"] = marketIds;
                    if (orderProjection != null) Filtr["orderProjection"] = orderProjection;
                    if (dateRange != null) Filtr["dateRange"] = dateRange;
                    if (orderBy != null) Filtr["orderBy"] = orderBy;
                    if (sortDir != null) Filtr["sortDir"] = sortDir;
                    Filtr["fromRecord"] = fromRecord;
                    Filtr["recordCount"] = recordCount;
                    return await Invoke<CurrentOrderSummaryReport>("listCurrentOrders", Filtr);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "C[Betfair]F[listCurrentOrders]");
                    return null;
                }
            }
            #endregion
            #region listClearedOrders
            public async Task<ClearedOrderSummaryReport> listClearedOrders(Betfair.BetStatus betStatus,FlitrByIds f1=null,FiltrBySTGI f2=null, int fromRecord = 0, int recordCount=0)
            {
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["betStatus"] = betStatus;
                if (f1 != null)
                {
                    if (f1.betIds != null) Filtr["betIds"] = f1.betIds;
                    if (f1.eventIds != null) Filtr["eventIds"] = f1.eventIds;
                    if (f1.eventTypeIds != null) Filtr["eventTypeIds"] = f1.eventTypeIds;
                    if (f1.marketIds != null) Filtr["marketIds"] = f1.marketIds;
                    if (f1.runnerIds != null) Filtr["runnerIds"] = f1.runnerIds;
                }
                if (f2 != null)
                {
                    if (f2.groupBy != null) Filtr["groupBy"] = f2.groupBy;
                    if (f2.includeItemDescription != null) Filtr["includeItemDescription"] = f2.includeItemDescription;
                    if (f2.settledDateRange != null) Filtr["settledDateRange"] = f2.settledDateRange;
                    if (f2.side != null) Filtr["side"] = f2.side;
                }
                if (fromRecord != 0) Filtr["fromRecord"] = fromRecord;
                if (recordCount != 0) Filtr["recordCount"] = recordCount;
                return await Invoke<ClearedOrderSummaryReport>("listClearedOrders", Filtr);
            }
            public class FlitrByIds
            {
                public string[] eventTypeIds;
                public string[] eventIds;
                public string[] marketIds;
                public RunnerId[] runnerIds;
                public string[] betIds;
            }
            public class FiltrBySTGI
            {
                public Order.Side? side;
                public TimeRange settledDateRange;
                public GroupBy? groupBy;
                public bool? includeItemDescription;
            }
            #endregion
            #region listMarketProfitAndLoss
            public async Task<MarketProfitAndLoss[]> listMarketProfitAndLoss(string[] marketIds, bool includeSettledBets = false, bool includeBspBets = false, bool netOfCommission=false)
            {
                if (marketIds == null) return null;
                IDictionary<string, object> Filtr = new Dictionary<string, object>();
                Filtr["marketIds"] = marketIds;
                if (includeSettledBets) Filtr["includeSettledBets"] = true;
                if (includeBspBets) Filtr["includeBspBets"] = true;
                if (netOfCommission) Filtr["netOfCommission"] = true;
                return await InvokeArr<MarketProfitAndLoss>("listMarketProfitAndLoss", Filtr);
            }
            #endregion
            #region getAccountFunds
            public Task<AccountFundsResponse> getAccountFunds(Wallet wa = Wallet.UK)
            {
                IDictionary<string,object> Params=new Dictionary<string,object>();
                Params["wallet"] = wa;
                return Invoke<AccountFundsResponse>("getAccountFunds",Params);
            }
            #endregion
            #region Commission
            public async Task<double?> getCommission()
            {
                AccountFundsResponse resp = await getAccountFunds();
                if (resp == null)
                {
                    for (; MessageBox.Show("Commission is empty, try again?", "Commission", MessageBoxButtons.YesNo) == DialogResult.Yes; ) 
                    {
                        resp =await getAccountFunds();
                        if (resp == null) continue;
                        else break;
                    }
                    if (resp == null) return null;
                }
                return -7 * (1 - resp.discountRate / 100);
            }
            #endregion
        #endregion
    }
    #endregion
    #region SQL_DB AND DB_TABLES
    public class DB_IExchange : DataContext
    {
        public Table<MarketTypes> marketTypes;
        public DB_IExchange(string ServerName) : base(ServerName) { }
    }
    #region DB Tables
    [Table(Name = "MarketTypes")]
    public class MarketTypes
    {
        [Column(Name = "Id", DbType = "int IDENTITY(1,1)", IsPrimaryKey = true,IsDbGenerated=true)]
        public int ID;

        [Column(Name = "SportID", DbType = "nvarchar(20) NOT NULL")]
        public string SportID;

        [Column(Name = "Sport", DbType = "nvarchar(200) NOT NULL")]
        public string SportName;

        [Column(Name = "MarketTypeName", DbType = "nvarchar(200) NOT NULL")]
        public string TypeName;

        [Column(Name = "BettingType", DbType = "nvarchar(50) NOT NULL")]// CONSTRAINT  BT CHECK (BettingType IN ('ODDS','ASIAN_HANDICAP_DOUBLE_LINE','ASIAN_HANDICAP_SINGLE_LINE'))")]
        public string BettingType;

        [Column(Name = "RowsCount", DbType = "int NOT NULL")]
        public int RowsCount;

        [Column(Name = "ActivRowsCount", DbType = "int NOT NULL")]
        public int ActivRowsCount;

        [Column(Name = "WinersCount", DbType = "int NOT NULL")]
        public int WinersCount;

    }
    #endregion
    #endregion
    #region RMBetfair
    public class RMBetfair : RMath.IExchange
    {
        private RMBetfair() { ConnectingDB(); }
        private static Betfair BF=null;
        public static RMath.IExchange Initialize(Betfair bf)
        {
            if (BF != null) return null;
            BF = bf;
            RMMarket.Initialize(bf);
            return new RMBetfair();
        }
        bool RMath.IExchange.ConectionStatus
        {
            get { return BF.LoginStatus; }
        }
        DB_IExchange DB = null;
        #region Update Market Type Data
        public void AutoUpdateMarketTypeData(int Interval_Hour)
        {
            System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
            timer.Interval = Interval_Hour * 60 * 60 * 1000;
            timer.Tick += timer_Tick;
        }
        async void timer_Tick(object sender, EventArgs e)
        {
            if (!await UpdateMarketTypeData())
            {
                DialogResult res = MessageBox.Show("Can't update market type data", "Auto Update Market Type Data", MessageBoxButtons.RetryCancel, MessageBoxIcon.Warning);
                if (res == DialogResult.Retry) timer_Tick(null,null);
            }
        }
        class UMTD
        {
            public string Sport_ID;
            public string Sport_name;
            public UMTD_MarketData[] MarketsData;
            public class UMTD_MarketData
            {
                public string MarketT;
                public Betfair.MarketCatalogue.MarketDescription.MarketBettingType BetingT;
                public int RowsCount;
                public int ActivRowsCount;
                public int WinersCount;
            }
        }
        public void ConnectingDB()
        {
            if (DB == null)
            {
                try
                {
                    DB = new DB_IExchange(@"GUGO-PC\SQLEXPRESS");
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Error SQL Server", MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                if (!DB.DatabaseExists())
                {
                    DialogResult res = MessageBox.Show("Not Found DB, Create DB?", "SQL DB", MessageBoxButtons.YesNo, MessageBoxIcon.Question);
                    if (res == DialogResult.Yes)
                    {
                        try
                        {
                            DB.CreateDatabase();
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show(ex.Message, "SQL DB", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        }
                        MessageBox.Show("DB is Created", "SQL DB", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
            }
        }
        public async Task<bool> UpdateMarketTypeData()
        {
            if (BF.LoginStatus)
            {
                Betfair.EventTypeResult[] SportsType = await BF.getEventTyps(Betfair.EmptyFiltr);
                List<UMTD> MData = new List<UMTD>();
                foreach (var el in SportsType)
                {
                    string SportID = el.eventType.id;
                    string SportName = el.eventType.name;
                    Betfair.MarketFiltr MFiltr = new Betfair.MarketFiltr();
                    MFiltr.AddFiltr(Betfair.MarketFiltr.FiltrNames.eventTypeIds, new string[] { SportID });
                    
                    #region Get Markets Type By Sport ID
                    Betfair.MarketTypeResult[] MarketsType = await BF.getMarketTypes(MFiltr.Filtr);
                    #endregion
                    #region Get Market Catalogue By Sport ID and Market Type
                    Task<Betfair.MarketCatalogue[]>[] MarketsCatalogue = new Task<Betfair.MarketCatalogue[]>[MarketsType.Length];
                    for (int i = 0; MarketsType != null && i < MarketsType.Length; i++) 
                    {
                        MFiltr.AddFiltr(Betfair.MarketFiltr.FiltrNames.marketTypeCodes, new string[] { MarketsType[i].marketType });
                        MarketsCatalogue[i] = BF.getMarketCatalogue(MFiltr.Filtr, 1, Betfair.MarketFiltr.MarketProjection.selectAll(), Betfair.MarketFiltr.MarketSort.MAXIMUM_TRADED);
                    }
                    Task.WaitAll(MarketsCatalogue);
                    #endregion
                    #region Get Market Book By Market ID
                    Task<Betfair.MarketBook[]>[] MarketsBook = new Task<Betfair.MarketBook[]>[MarketsType.Length];
                    for (int i = 0; MarketsType != null && i < MarketsType.Length; i++) 
                    {
                        if (MarketsCatalogue[i].Result == null) continue;
                        if (MarketsCatalogue[i].Result[0].marketId == null)
                        {
                            continue;
                        }
                        MarketsBook[i] = BF.listMarketBook(new string[] { MarketsCatalogue[i].Result[0].marketId });
                    }
                    Task.WaitAll(MarketsBook);
                    #endregion
                    #region Add to ArrayList
                    List<UMTD.UMTD_MarketData> temp=new List<UMTD.UMTD_MarketData>();
                    for (int i = 0; MarketsType != null && i < MarketsType.Length; i++)
                    {
                        if (MarketsBook[i].Result == null || MarketsCatalogue[i].Result == null || MarketsType[i] == null || MarketsCatalogue[i].Result[0].description == null) continue;
                        temp.Add( new UMTD.UMTD_MarketData
                        {
                            MarketT = MarketsType[i].marketType,
                            BetingT = MarketsCatalogue[i].Result[0].description.bettingType,
                            RowsCount = MarketsBook[i].Result[0].numberOfRunners == null ? 0 :(int) MarketsBook[i].Result[0].numberOfRunners,
                            ActivRowsCount = MarketsBook[i].Result[0].numberOfActiveRunners == null ? 0 : (int)MarketsBook[i].Result[0].numberOfActiveRunners,
                            WinersCount = MarketsBook[i].Result[0].numberOfWinners == null ? 0 : (int)MarketsBook[i].Result[0].numberOfWinners
                        });
                    }
                    MData.Add(new UMTD
                    {
                        Sport_ID = SportID,
                        Sport_name=SportName,
                        MarketsData = temp.ToArray()
                    });
                    #endregion
                }
                try
                {
                    foreach (var Sport in MData)
                    {
                        foreach(var Market in Sport.MarketsData )
                        {
                            #region Incorrect Data
                            if (DB.marketTypes.Select(_ => new { _.TypeName, _.SportID }).Any(_ => _.TypeName == Market.MarketT && _.SportID == Sport.Sport_ID)) 
                            {
                                var IncorrectEl = DB.marketTypes.Where(_
                                    => _.TypeName == Market.MarketT && _.SportID == Sport.Sport_ID
                                    && (_.RowsCount != Market.RowsCount
                                    || _.ActivRowsCount!=Market.ActivRowsCount
                                    || _.WinersCount != Market.WinersCount
                                    || _.BettingType != Market.BetingT.ToString()));
                                if (IncorrectEl != null && IncorrectEl.Count() > 0)
                                {
                                    var el = IncorrectEl.First();
                                    string WarningMessageDB = "\nDB ID: " + el.ID + "\nMarket Type: " + el.TypeName + "\nSport ID:" + el.SportID + "\nDB Data: R_Count[" + el.RowsCount + "] A_R_Count["+el.ActivRowsCount+"] W_Count[" + el.WinersCount + "] Beting Type[" + el.BettingType + "]";
                                    string WarningMessageAPI = "\nAPI Data: R_Count[" + Market.RowsCount + "] A_R_Count["+Market.ActivRowsCount+"] W_Count[" + Market.WinersCount + "] Beting Type[" + Market.BetingT + "]";
                                    DialogResult resM = MessageBox.Show("Incorrect data in DB Update it?" + WarningMessageDB + WarningMessageAPI, "Update Market Type Data", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
                                    if (resM == DialogResult.Yes)
                                    {
                                        el.RowsCount = Market.RowsCount;
                                        el.WinersCount = Market.WinersCount;
                                        el.BettingType = Market.BetingT.ToString();
                                    }
                                }
                                continue;
                            }
                            #endregion
                            DB.marketTypes.InsertOnSubmit(new MarketTypes()
                            {
                                TypeName = Market.MarketT,
                                WinersCount = Market.WinersCount,
                                RowsCount = Market.RowsCount,
                                ActivRowsCount=Market.ActivRowsCount,
                                BettingType = Market.BetingT.ToString(),
                                SportID=Sport.Sport_ID,
                                SportName=Sport.Sport_name
                                
                            });
                        }
                    }
                    DB.SubmitChanges();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Class[RMBetfair], Region[Update Market Type Data], Function[UpdateMarketTypeData()], \nMessage: " + ex.Message, "ERROR");
                }
            }
            else
            {
                MessageBox.Show("Please Login Betfair end try again", "Betfair Login");
                return false;
            }
            return true;
        }
        #endregion
        async Task<List<RMath.MFiltre>> RMath.IExchange.GetAllMarketsType()
        {
            if (DB.marketTypes.Count() > 0) AutoUpdateMarketTypeData(3);
            else await UpdateMarketTypeData();
            if (BF.LoginStatus)
            {
                List<RMath.MFiltre> MFiltres = new List<RMath.MFiltre>();
                return DB.marketTypes.Select(_ => new RMath.MFiltre(_.TypeName,_.SportID,_.SportName,_.ActivRowsCount, _.RowsCount, _.WinersCount, _.BettingType == "ODDS" ? RMath.MFiltre.BetingType.ODDS : _.BettingType == "ASIAN_HANDICAP_SINGLE_LINE" ? RMath.MFiltre.BetingType.AH_S : RMath.MFiltre.BetingType.AH_D)).ToList();
            }
            return null;
        }
        async Task<List<RMath.IMarket>> RMath.IExchange.GetAllMarkets(List<RMath.MFiltre> marketFiltre)
        {
            #region Get Events by MarketFiltr
            var MarketsFiltr=marketFiltre.Select(_ => _.MarketTypeName).ToList();
            Betfair.MarketFiltr EventsFiltr = new Betfair.MarketFiltr();
            EventsFiltr.AddFiltr(Betfair.MarketFiltr.FiltrNames.marketTypeCodes, MarketsFiltr);
            EventsFiltr.AddFiltr(Betfair.MarketFiltr.FiltrNames.inPlayOnly, true);
            //EventsFiltr.AddFiltr(Betfair.MarketFiltr.FiltrNames.turnInPlayEnabled, false);
            List<Betfair.EventResult> eventRes = null;
            try
            {
                eventRes = (await BF.getEvens(EventsFiltr.Filtr)).ToList();
            }
            catch (Exception Ex)
            {
                MessageBox.Show(Ex.Message,"C[RMBetfair]F[GetAllMarkets]R[G_E_b_MF]");
            }
           // Betfair.MarketFiltr.MarketProjection marketProjection = Betfair.MarketFiltr.MarketProjection.MARKET_DESCRIPTION + Betfair.MarketFiltr.MarketProjection.RUNNER_DESCRIPTION;
            Betfair.MarketFiltr.MarketProjection marketProjection = Betfair.MarketFiltr.MarketProjection.RUNNER_DESCRIPTION;
            int MaxMarketsResult = marketProjection.getMaxResult();
            eventRes.Sort((a, b) =>/*1->0*/
            {
                if (a.marketCount > b.marketCount) return -1;
                else if (a.marketCount < b.marketCount) return 1;
                return 0;
            });
            #endregion
            #region Grouping Results
            if ((int)eventRes.Max(_ => _.marketCount) > MaxMarketsResult)//#1
            {
                MessageBox.Show("Pleas Add Cod in Function>RMBetfair.GetAllMarkets()#1\n[EventMarketCount > MaxMarketResult]", "C[RMBetfair]F[GetAllMarkets]R[G_R]", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            List<List<Betfair.EventResult>> grups = new List<List<Betfair.EventResult>>();
            List<Betfair.EventResult> temp = new List<Betfair.EventResult>();
            for (int i = 0, CountSum = 0; ; i++)
            {
                if (i == eventRes.Count)
                {
                    grups.Add(temp);
                    break;
                }
                if (CountSum + (int)eventRes[i].marketCount < MaxMarketsResult)
                {
                    temp.Add(eventRes[i]);
                    CountSum += (int)eventRes[i].marketCount;
                }
                else
                {
                    i--;
                    var haveEv = eventRes.Skip(i + 1).First(_ => _.marketCount + CountSum <= MaxMarketsResult);
                    if (haveEv != null)
                    {
                        temp.Add(haveEv);
                        eventRes.Remove(haveEv);
                        CountSum += (int)haveEv.marketCount;
                        if (CountSum != MaxMarketsResult) continue;
                    }

                    CountSum = 0;
                    grups.Add(temp);
                    temp = new List<Betfair.EventResult>();
                }
            }
            #endregion
            #region Get MarketS Catalogue
            List<Betfair.MarketCatalogue> Markets = new List<Betfair.MarketCatalogue>();
            Betfair.MarketFiltr Filtr;
            List<Task<Betfair.MarketCatalogue[]>> resp = new List<Task<Betfair.MarketCatalogue[]>>();
            foreach (var req in grups)
            {
                Filtr = new Betfair.MarketFiltr();
                Filtr.AddFiltr(Betfair.MarketFiltr.FiltrNames.marketTypeCodes, MarketsFiltr.ToArray());
                Filtr.AddFiltr(Betfair.MarketFiltr.FiltrNames.inPlayOnly, true);
                Filtr.AddFiltr(Betfair.MarketFiltr.FiltrNames.eventIds, req.Select(_ => _.@event.id));
                try
                {
                    resp.Add(BF.getMarketCatalogue(Filtr.Filtr, MaxMarketsResult, marketProjection));
                }
                catch (Exception Ex)
                {
                    MessageBox.Show(Ex.Message, "C[RMBetfair]F[GetAllMarkets]R[G_M_C]");
                }
            }
            Task.WaitAll(resp.ToArray());
            foreach (var el in resp) Markets.AddRange(el.Result);
            #endregion
            return Markets.Select(_ =>
            {
                if (_ == null || _.marketId == null || _.marketId.Length == 0) 
                {
                    MessageBox.Show("");
                }
                return (RMath.IMarket)new RMMarket(_, null);
            }).ToList();
        }
        private sealed class RMMarket : RMath.IMarket
        {
            private static Betfair BF;
            private readonly Betfair.MarketCatalogue MarketRef;
            public RMMarket(Betfair.MarketCatalogue Market, RMath.MFiltre MF) { this.MarketRef = Market; this.MarketFiltre = MF; }
            public static void Initialize(Betfair bf) { BF = bf; }
            async Task<bool> RMath.IMarket.PlaceBet(int row, bool BackOrLay, double kc, double stake)
            {

                Betfair.PlaceInstruction instruction=new Betfair.PlaceInstruction();
                instruction.selectionId = MarketRef.runners[row].selectionId;
                instruction.orderType = Betfair.Order.OrderType.LIMIT;
                if(BackOrLay)instruction.side = Betfair.Order.Side.BACK;
                else instruction.side = Betfair.Order.Side.LAY;
                instruction.limitOrder = new Betfair.PlaceInstruction.LimitOrder();
                instruction.limitOrder.persistenceType = Betfair.Order.PersistenceType.LAPSE;
                instruction.limitOrder.price = kc;
                instruction.limitOrder.size = stake;
                if (stake < 4)
                {
                    instruction.limitOrder.size = 4;
                    if (BackOrLay) instruction.limitOrder.price = 1000;
                    else instruction.limitOrder.price = 1.01;
                }
                List<Betfair.PlaceInstruction> instructions = new List<Betfair.PlaceInstruction>();
                instructions.Add(instruction);
                Betfair.PlaceExecutionReport resp = await BF.placeOrders(MarketRef.marketId, instructions);
                if (resp.errorCode == null)
                {
                    if (stake < 4)
                    {
                        Betfair.CancelInstruction[] ci = { new Betfair.CancelInstruction() };
                        ci[0].betId = resp.instructionReports[0].betId;
                        ci[0].sizeReduction = 4 - stake;
                        var respcancel= await BF.cancelOrders(MarketRef.marketId, ci);
                        if (respcancel.errorCode != null) return false;
                        Betfair.ReplaceInstruction[] ri ={ new Betfair.ReplaceInstruction()};
                        ri[0].betId = ci[0].betId;
                        ri[0].newPrice = kc;
                        var respR = await BF.replaceOrders(MarketRef.marketId, ri);
                        if (respR.errorCode != null) return false;
                    }
                    return true;
                }
                return false;
            }
            bool RMath.IMarket.Stop()
            {
                return false;
            }
            double RMath.IMarket.nextKC(double kc, bool BackOrLay)
            {
                if (MarketRef.marketName == "ASIAN_HANDICAP")
                {
                    if (BackOrLay) return Betfair.nextAsianKcBack(kc);
                    return Betfair.nextAsianKcLay(kc);
                }
                if (BackOrLay) return Betfair.nextKcBack(kc);
                return Betfair.nextKcLay(kc);
            }
            async Task<List<RMath.Row>> RMath.IMarket.BestRows()
            {
                Betfair.PriceProjection filtr = new Betfair.PriceProjection();
                filtr.SetPriceData(Betfair.PriceProjection.PriceData.EX_BEST_OFFERS);
                Betfair.MarketBook[] resp = await BF.OlistMarketBook(new string[] { MarketRef.marketId }, filtr);
                if (resp == null || resp[0] == null || resp[0].runners == null || resp[0].runners.Count == 0
                    || resp[0].runners.Any(_ => _.ex.availableToBack.Count == 0 || _.ex.availableToLay.Count == 0)) return null;
                Betfair.MarketBook market = resp[0];
                if (market == null || market.runners == null) return null;
                List<RMath.Row> rows = new List<RMath.Row>();
                foreach (Betfair.MarketBook.Runner row in market.runners)
                {
                    List<RMath.Row.KcAndStake> rback=row.ex.availableToBack.Select(_ => new RMath.Row.KcAndStake(_.price, _.size)).ToList();
                    List<RMath.Row.KcAndStake> rlay=row.ex.availableToLay.Select(_ => new RMath.Row.KcAndStake(_.price, _.size)).ToList();
                    rows.Add(new RMath.Row(rback, rlay));
                }
                last_BestRows = rows;
                bestRows_UpdateTime = DateTime.Now;
                return rows;
            }
            async Task<List<RMath.Row>> RMath.IMarket.AllRows()
            {
                Betfair.PriceProjection filtr = new Betfair.PriceProjection();
                filtr.SetPriceData(Betfair.PriceProjection.PriceData.EX_ALL_OFFERS);
                Betfair.MarketBook[] resp = await BF.listMarketBook(new string[] { MarketRef.marketId }, filtr);
                Betfair.MarketBook market = resp[0];
                List<RMath.Row> rows = new List<RMath.Row>();
                foreach (Betfair.MarketBook.Runner row in market.runners)
                {
                    List<RMath.Row.KcAndStake> rback = row.ex.availableToBack.Select(_ => new RMath.Row.KcAndStake(_.price, _.size)).ToList();
                    List<RMath.Row.KcAndStake> rlay = row.ex.availableToLay.Select(_ => new RMath.Row.KcAndStake(_.price, _.size)).ToList();
                    rows.Add(new RMath.Row(rback, rlay));
                }
                last_AllRows = rows;
                allRows_UpdateTime = DateTime.Now;
                return rows;
            }
            private RMath.MFiltre MarketFiltre;
            RMath.MFiltre RMath.IMarket.MF()
            {
                return MarketFiltre;
            }
            async Task<RMath.IEOrders> RMath.IMarket.MarketOrders()
            {
                #region ProfitAndLoss
                Betfair.MarketProfitAndLoss[] respPL = await BF.listMarketProfitAndLoss(new string[] { MarketRef.marketId });
                List<Betfair.MarketProfitAndLoss.RunnerProfitAndLoss> Runners= respPL[0].profitAndLosses.ToList();
                Dictionary<long,double> ProfitAndLoss=new Dictionary<long,double>();
                foreach (var el in Runners) ProfitAndLoss.Add((long)el.selectionId, (double)el.ifWin);
                #endregion
                #region Orders
                Betfair.CurrentOrderSummaryReport resp = await BF.listCurrentOrders(null, new string[] { this.MarketRef.marketId }, Betfair.OrderProjection.ALL, null, null, null, 0, 1000);
                List<RMMOrders.OrderMatched> Matched;
                List<RMMOrders.OrderUnMatched> UnMatched;
                List<Betfair.CurrentOrderSummaryReport.CurrentOrderSummary> AllOrders = new List<Betfair.CurrentOrderSummaryReport.CurrentOrderSummary>(resp.currentOrders);
                for (int i = 1000; resp.moreAvailable; i += 1000)
                {
                    resp = await BF.listCurrentOrders(null, new string[] { this.MarketRef.marketId }, Betfair.OrderProjection.ALL, null, null, null, i, 1000);
                    AllOrders.AddRange(resp.currentOrders);
                }
                Matched = AllOrders.Where(_ => _.status == Betfair.Order.OrderStatus.EXECUTION_COMPLETE)
                    .Select(_ => new RMMOrders.OrderMatched(_.marketId, _.betId, _.selectionId, (_.side == Betfair.Order.Side.BACK ? true : false), (double)_.averagePriceMatched, (double)_.sizeMatched, _.placedDate)).ToList();
                UnMatched = AllOrders.Where(_ => _.status == Betfair.Order.OrderStatus.EXECUTABLE)
                    .Select(_ => new RMMOrders.OrderUnMatched(_.marketId, _.betId, _.selectionId, (_.side == Betfair.Order.Side.BACK ? true : false), (double)_.priceSize.price, (double)_.sizeRemaining, _.placedDate)).ToList();
                RMMOrders orders = new RMMOrders(this.MarketRef.marketId, ProfitAndLoss, Matched, UnMatched);
                #endregion
                return orders;
            }
            public class RMMOrders : RMath.IEOrders
            {
                private string marketID;
                public RMMOrders(string MarketID,Dictionary<long, double> Profit,List<OrderMatched> OM,List<OrderUnMatched> OUM)
                {
                    this.marketID = MarketID;
                    this.profit = Profit;
                    this.orderMatched = new List<RMath.IEOrderMatched>(OM.ToArray());
                    this.orderUnMatched = new List<RMath.IEOrderUnMatched>(OUM.ToArray());
                }
                private Dictionary<long, double> profit;
                Dictionary<long, double> RMath.IEOrders.MarketProfit
                {
                    get { return this.profit; }
                }
                private List<RMath.IEOrderMatched> orderMatched;
                List<RMath.IEOrderMatched> RMath.IEOrders.Matched
                {
                    get { return this.orderMatched; }
                }
                private List<RMath.IEOrderUnMatched> orderUnMatched;
                List<RMath.IEOrderUnMatched> RMath.IEOrders.UnMatched
                {
                    get { return this.orderUnMatched; }
                }
                async Task<bool> RMath.IEOrders.Refresh()
                {
                    Betfair.CurrentOrderSummaryReport resp = await BF.listCurrentOrders(null, new string[] { this.marketID }, Betfair.OrderProjection.ALL, null, null, null, 0, 1000);
                    if (resp == null) return false;
                    List<RMath.IEOrderMatched> Matched;
                    List<RMath.IEOrderUnMatched> UnMatched;
                    List<Betfair.CurrentOrderSummaryReport.CurrentOrderSummary> AllOrders = new List<Betfair.CurrentOrderSummaryReport.CurrentOrderSummary>(resp.currentOrders);
                    for (int i = 1000; resp.moreAvailable; i += 1000) 
                    {
                        resp = await BF.listCurrentOrders(null, new string[] { this.marketID }, Betfair.OrderProjection.ALL, null, null, null, i, 1000);
                        AllOrders.AddRange(resp.currentOrders);
                    }
                    Matched = AllOrders.Where(_ => _.status == Betfair.Order.OrderStatus.EXECUTION_COMPLETE).Select(_ => (RMath.IEOrderMatched)(new OrderMatched(_.marketId, _.betId, _.selectionId, (_.side == Betfair.Order.Side.BACK ? true : false), (double)_.averagePriceMatched, (double)_.sizeMatched, _.placedDate))).ToList();
                    UnMatched = AllOrders.Where(_ => _.status == Betfair.Order.OrderStatus.EXECUTABLE).Select(_ => (RMath.IEOrderUnMatched)(new OrderMatched(_.marketId, _.betId, _.selectionId, (_.side == Betfair.Order.Side.BACK ? true : false), (double)_.priceSize.price, (double)_.sizeRemaining, _.placedDate))).ToList();
                    this.orderMatched = Matched;
                    this.orderUnMatched = UnMatched;
                    return true;
                }
                async Task<bool> RMath.IEOrders.CanselAllUnMatchedOrders()
                {
                    Betfair.CancelExecutionReport resp = await BF.cancelOrders(this.marketID, null);
                    if (resp.errorCode != null) return false;
                    return true;
                }
                public class OrderMatched : RMath.IEOrderMatched
                {
                    protected string marketID;
                    public OrderMatched(string MarketID, string OrderID, long RowID, bool BackOrLay, double Kc, double Stake, DateTime PlacedTime)
                    {
                        this.marketID = MarketID;
                        this.orderID = OrderID;
                        this.rowID = RowID;
                        this.backOrLay = BackOrLay;
                        this.kc = Kc;
                        this.stake = Stake;
                        this.placedTime = PlacedTime;
                    }
                    protected string orderID;
                    string RMath.IEOrderMatched.OrderID
                    {
                        get { return this.orderID; }
                    }
                    protected DateTime placedTime;
                    DateTime RMath.IEOrderMatched.PlacedTime
                    {
                        get { return this.placedTime; }
                    }
                    protected long rowID;
                    long RMath.IEOrderMatched.RowID
                    {
                        get { return this.rowID; }
                    }
                    protected bool backOrLay;
                    bool RMath.IEOrderMatched.BackOrLay
                    {
                        get { return this.backOrLay; }
                    }
                    protected double kc;
                    double RMath.IEOrderMatched.Kc
                    {
                        get { return this.kc; }
                    }
                    protected double stake;
                    double RMath.IEOrderMatched.Stake
                    {
                        get { return this.stake; }
                    }
                }
                public class OrderUnMatched : OrderMatched, RMath.IEOrderUnMatched
                {
                    public OrderUnMatched(string MarketID, string OrderID, long RowID, bool BackOrLay, double Kc, double Stake, DateTime PlacedTime)
                        :base(MarketID, OrderID,RowID,BackOrLay,Kc,Stake,PlacedTime){}
                    async Task<double?> RMath.IEOrderUnMatched.CanselOrder(double? Stake)
                    {
                        Betfair.CancelInstruction[] instructin = new Betfair.CancelInstruction[] { new Betfair.CancelInstruction() };
                        instructin[0].betId = this.orderID;
                        instructin[0].sizeReduction = Stake;
                        Betfair.CancelExecutionReport resp =await BF.cancelOrders(this.marketID,instructin);
                        if (resp.errorCode != null) return null;
                        return resp.instructionReports[0].sizeCancelled;
                    }
                    async Task<bool> RMath.IEOrderUnMatched.ReplaceOrder(double Kc)
                    {
                        Betfair.ReplaceInstruction[] instruction = new Betfair.ReplaceInstruction[]{new Betfair.ReplaceInstruction()};
                        instruction[0].betId = this.orderID;
                        instruction[0].newPrice = kc;
                        Betfair.ReplaceExecutionReport resp = await BF.replaceOrders(this.marketID, instruction);
                        if (resp.errorCode != null) return false;
                        return true;
                    }
                }
            }
            private List<RMath.Row> last_BestRows;
            List<RMath.Row> RMath.IMarket.Last_BestRows
            {
                get { return this.last_BestRows; }
            }
            private DateTime bestRows_UpdateTime;
            DateTime RMath.IMarket.BestRows_UpdateTime
            {
                get { return bestRows_UpdateTime; }
            }
            private List<RMath.Row> last_AllRows;
            List<RMath.Row> RMath.IMarket.Last_AllRows
            {
                get { return last_AllRows; }
            }
            private DateTime allRows_UpdateTime;
            DateTime RMath.IMarket.AllRows_UpdateTime
            {
                get { return allRows_UpdateTime; }
            }
            Dictionary<int, double> RMath.IMarket.MarketProfitPercent(bool BestRows_Or_AllRows,List<RMath.Row> ROWS)
            {
                double kc1, kc2, kc3, l1, l2, l3;
                if (BestRows_Or_AllRows)//BestRows
                {
                    var LBR = this.last_BestRows;
                    if (ROWS != null) LBR = ROWS;
                    if (LBR == null || LBR.Count() < 2 || LBR.Any(_ => _.Back == null || _.Lay == null || _.Back.Count == 0 || _.Lay.Count == 0 || _.Back[0].Kc <= 1 || _.Lay[0].Kc <= 1)) return null;
                    kc1 = this.last_BestRows[0].Back[0].Kc;
                    l1 = this.last_BestRows[0].Lay[0].Kc;
                    kc2 = this.last_BestRows[1].Back[0].Kc;
                    l2 = this.last_BestRows[1].Lay[0].Kc;
                    kc3 = this.last_BestRows[2].Back[0].Kc;
                    l3 = this.last_BestRows[2].Lay[0].Kc;
                }
                else//AllRows
                {
                    var LBR = this.last_AllRows;
                    if (ROWS != null) LBR = ROWS;
                    if (LBR == null || LBR.Count() < 2 || LBR.Any(_ => _.Back == null || _.Lay == null || _.Back.Count == 0 || _.Lay.Count == 0 || _.Back[0].Kc <= 1 || _.Lay[0].Kc <= 1)) return null;
                    kc1 = this.last_AllRows[0].Back[0].Kc;
                    l1 = this.last_AllRows[0].Lay[0].Kc;
                    kc2 = this.last_AllRows[1].Back[0].Kc;
                    l2 = this.last_AllRows[1].Lay[0].Kc;
                    kc3 = this.last_AllRows[2].Back[0].Kc;
                    l3 = this.last_AllRows[2].Lay[0].Kc;
                }
                double[] L1Percent = new double[6]{
                    //Back KC
                     RMath.profitPercent(((RMath.IMarket)this).nextKC(kc1, true), kc2, kc3),//KC1 - 0
                     RMath.profitPercent(kc1, ((RMath.IMarket)this).nextKC(kc2, true), kc3),//KC2 - 1
                     RMath.profitPercent(kc1, kc2, ((RMath.IMarket)this).nextKC(kc3, true)),//KC3 - 2

                    //Lay KC
                     RMath.profitPercent(((RMath.IMarket)this).nextKC(l1, false), l2, l3, false),//L1 - 3
                     RMath.profitPercent(l1, ((RMath.IMarket)this).nextKC(l2, false), l3, false),//L2 - 4
                     RMath.profitPercent(l1, l2, ((RMath.IMarket)this).nextKC(l3, false), false),//L3 - 5
                    };
                Dictionary<int, double> res = new Dictionary<int, double>();
                for (int i = 0; i < 6; i++)
                {
                    if (L1Percent[i] > IExchange.MyClass.Client_Cod_Interface.MinProfitPercent) res.Add(i, L1Percent[i]);
                }
                LMPP = res;
                return res;
            }
            int IComparable.CompareTo(object obj)
            {
                RMath.IMarket IM = obj as RMath.IMarket;
                if (IM == null) return 0;
                if (this.LMPP != null && IM.LastMarketProfitPercent != null)
                {
                    if (this.LMPP.Sum(_ => _.Value) > IM.LastMarketProfitPercent.Sum(_ => _.Value)) return -1;
                    if (this.LMPP.Sum(_ => _.Value) < IM.LastMarketProfitPercent.Sum(_ => _.Value)) return 1;
                }
                return 0;
            }
            private Dictionary<int, double> LMPP;
            Dictionary<int, double> RMath.IMarket.LastMarketProfitPercent
            {
                get { return this.LMPP; }
            }
            RMath.IMarketStatus RMath.IMarket.IStatus
            {
                get;
                set;
            }
            bool RMath.IMarket.IStatusChanged
            {
                get;
                set;
            }
            async Task<RMath.ORDER> RMath.IMarket.PlaceBetO(int row3, bool BackOrLay, double kc, double stake)
            {
                Betfair.PlaceExecutionReport.PlaceInstructionReport RespInstruction = null;

                try
                {
                    #region Rerquest
                    double KC = Math.Round(kc, 2);
                    double Stake = Math.Round(stake, 2);
                    Betfair.PlaceInstruction request = new Betfair.PlaceInstruction();
                    request.orderType = Betfair.Order.OrderType.LIMIT;
                    request.selectionId = MarketRef.runners[row3].selectionId;
                    request.side = BackOrLay == true ? Betfair.Order.Side.BACK : Betfair.Order.Side.LAY;
                    request.limitOrder = new Betfair.PlaceInstruction.LimitOrder();
                    request.limitOrder.persistenceType = Betfair.Order.PersistenceType.LAPSE;
                    request.limitOrder.price = Stake >= 4 ? KC : BackOrLay ? 1000 : 1.01;
                    request.limitOrder.size = Math.Round(Stake < 4 ? 4 : Stake, 2);
                    #endregion
                    var resp = await BF.placeOrders(MarketRef.marketId, new List<Betfair.PlaceInstruction>() { request });
                    if (resp == null || resp.errorCode != null || resp.instructionReports == null)
                    {
                        return null;
                    }
                    RespInstruction = resp.instructionReports[0];
                    if (Stake < 4)
                    {
                        Betfair.CancelInstruction reqC = new Betfair.CancelInstruction() { betId = resp.instructionReports[0].betId, sizeReduction = Math.Round(4 - Stake, 2) };
                        var respC = await BF.cancelOrders(MarketRef.marketId, new Betfair.CancelInstruction[] { reqC });
                        if (respC == null || respC.errorCode != null || respC.instructionReports == null) return null;
                        Betfair.ReplaceInstruction reqI = new Betfair.ReplaceInstruction() { betId = resp.instructionReports[0].betId, newPrice = KC };
                        var respI = await BF.replaceOrders(MarketRef.marketId, new Betfair.ReplaceInstruction[] { reqI });
                        if (respI == null || respI.errorCode != null || respI.instructionReports == null) return null;
                        RespInstruction = respI.instructionReports[0].placeInstructionReport;
                    }
                }
                catch(Exception Ex)
                {
                    MessageBox.Show(Ex.Message, "C[RMMarket]F[PlaceBetO]");
                }
                return (RMath.ORDER)new ClassOrder(RespInstruction, MarketRef.marketId, row3);
            }
            public class ClassOrder:RMath.ORDER
            {
                private Betfair.PlaceExecutionReport.PlaceInstructionReport PlaceResponse;
                private Betfair.CurrentOrderSummaryReport.CurrentOrderSummary OrderResponse;
                private string MarketID;
                public ClassOrder(Betfair.PlaceExecutionReport.PlaceInstructionReport PlaceResp,string marketID,int Row)
                {
                    this.PlaceResponse = PlaceResp;
                    this.MarketID = marketID;
                    this.row = Row;
                    this.kc = PlaceResp.instruction.limitOrder.price;
                    this.size = PlaceResp.instruction.limitOrder.size;
                    this.backOrLay = PlaceResp.instruction.side == Betfair.Order.Side.BACK ? true : false;
                    this.placeData = (DateTime)PlaceResp.placedDate;
                    ((RMath.ORDER)this).UpdateOrder();
                }
                private int row;
                int RMath.ORDER.Row3
                {
                    get { return this.row; }
                }
                private double kc;
                double RMath.ORDER.OrderKC
                {
                    get { return this.kc; }
                }
                private double size;
                double RMath.ORDER.OrderSize
                {
                    get { return this.size; }
                }
                private bool backOrLay;
                bool RMath.ORDER.BackOrLay
                {
                    get { return this.backOrLay; }
                }
                private DateTime placeData;
                DateTime RMath.ORDER.PlacedData
                {
                    get { return placeData; }
                }
                private double? allMatchedSize = null;
                double? RMath.ORDER.AllMatchedSize
                {
                    get { return allMatchedSize; }
                }
                private double? newMatchedSize = null;
                double? RMath.ORDER.NewMatchedSize
                {
                    get
                    {
                       return this.newMatchedSize;
                    }
                }

                async Task<bool> RMath.ORDER.UpdateOrder()
                {
                    Betfair.CurrentOrderSummaryReport.CurrentOrderSummary[] resp = null;
                    try
                    {
                        resp = await BF.OlistCurrentOrders(new string[] { this.PlaceResponse.betId });
                        if (resp == null || resp.Length == 0 || resp[0] == null) return false;
                        this.OrderResponse = resp[0];
                        if (this.allMatchedSize == null)
                        {
                            allMatchedSize = OrderResponse.sizeMatched < 0.001 ? null : OrderResponse.sizeMatched;
                            newMatchedSize = allMatchedSize < 0.001 ? null : allMatchedSize;
                        }
                        else
                        {
                            newMatchedSize = OrderResponse.sizeMatched - allMatchedSize < 0.001 ? null : OrderResponse.sizeMatched - allMatchedSize;
                            allMatchedSize = OrderResponse.sizeMatched;
                        }
                    }
                    catch (Exception ex)
                    {
                        
                        MessageBox.Show(ex.Message, "C[ClassOrder]F[UpdateOrder()]");
                        return false;
                    }
                    return true;
                }

                async Task<bool> RMath.ORDER.CencelOrder(double? stake)
                {
                    try
                    {
                        var resp = await BF.cancelOrders(MarketID, new Betfair.CancelInstruction[] { new Betfair.CancelInstruction() { betId = PlaceResponse.betId, sizeReduction = stake } });
                        if (resp == null || resp.errorCode != null || resp.status != Betfair.ExecutionReportStatus.SUCCESS) return false;
                        if (stake == null) this.size = 0;
                        else this.size = this.size - (double)stake;
                    }
                    catch (Exception EX)
                    {
                        MessageBox.Show(EX.Message, "C[ClassOrder]F[CencelOrder()]");
                    }
                    return true;
                }
                void RMath.ORDER.NewMatchedSizeIzComplate()
                {
                    this.newMatchedSize = null;
                }
                int RMath.ORDER.Row5
                {
                    get { return backOrLay ? row : row + 3; }
                }


                async Task<bool> RMath.ORDER.ReplaseOrder(double KC)
                {
                    Betfair.ReplaceInstruction[] instruction = new Betfair.ReplaceInstruction[] { new Betfair.ReplaceInstruction() };
                    instruction[0].betId = this.PlaceResponse.betId;
                    instruction[0].newPrice = KC;
                    Betfair.ReplaceExecutionReport resp = await BF.replaceOrders(this.MarketID, instruction);
                    if (resp.errorCode != null) return false;
                    this.PlaceResponse = resp.instructionReports[0].placeInstructionReport;
                    return true;
                }
            }
            double? RMath.IMarket.getBestKC(int row5)
            {
                if (this.last_BestRows == null || this.last_BestRows.Count == 0) return null;
                if (row5 < 3) return this.last_BestRows[row5 % 3].Back[0].Kc;
                return this.last_BestRows[row5 % 3].Lay[0].Kc;
            }
            string RMath.IMarket.ID
            {
                get { return this.MarketRef.marketId; }
            }

            bool IEquatable<RMath.IMarket>.Equals(RMath.IMarket other)
            {
                return this.MarketRef.marketId == other.ID ? true : false;
            }
            public override int GetHashCode()
            {
                return this.MarketRef.marketId.GetHashCode();
            }
            private List<RMath.Row> mainRows = null;

            List<RMath.Row> RMath.IMarket.MainMarketRowsData
            {
                get
                {
                    return mainRows;
                }
                set
                {
                    mainRows = value;
                }
            }

            int RMath.IMarket.RowCount
            {
                get { return this.MarketRef.runners.Count; }
            }
        }
    }
#endregion
}
