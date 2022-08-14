using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows.Forms;
using System.Net;
using uBF = IExchange.Betfair.MarketFiltr;
using uBF_MP = IExchange.Betfair.MarketFiltr.MarketProjection;
namespace IExchange
{
    public partial class Form1 : Form
    {
        public Betfair BF;
        public Betfair_setings BS_Form;
        public Form1()
        {
            InitializeComponent();
            ServicePointManager.DefaultConnectionLimit = 10000;
            ServicePointManager.Expect100Continue = false;
            try { 
                BF = new Betfair();
                BS_Form = new Betfair_setings(BF);
                List<RMath.IExchange> Exchanges = new List<RMath.IExchange>() ;
                Exchanges.Add(RMBetfair.Initialize(BF));
                new Thread(() => { RMath.Initialize(Exchanges); }).Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                RMath.IEMathEnd();
            }
        }
        private void button1_Click(object sender, EventArgs e)
        {
            if (!BS_Form.Visible) BS_Form.Visible = true;
        }
        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            RMath.IEMathEnd();
        }

        private async void button3_Click(object sender, EventArgs e)
        {
            double? commission =await BF.getCommission();
            if (commission == null) return;
            this.Commission.Text = commission.ToString();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            Betfair.AccountFundsResponse resp = await BF.getAccountFunds();
            if (resp == null) return;
            this.ABalance.Text = resp.availableToBetBalance.ToString();
        }

        private void button4_Click(object sender, EventArgs e)
        {
            this.CanBeRuningMarketsCount.Text = (int.Parse(this.ABalance.Text) / int.Parse(this.LimitInMarket.Text)).ToString();
        }

        private void LimitInMarket_Validating(object sender, CancelEventArgs e)
        {
            double limit;
            if (!double.TryParse(this.LimitInMarket.Text, out limit)) e.Cancel = true;
            if (limit < 0) 
            {
                e.Cancel = true;
                MessageBox.Show("Limit in market must be greater than zero", "Limit in market", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void CanBeRuningMarketsCount_Validating(object sender, CancelEventArgs e)
        {
            int runingMarkets;
            if (!int.TryParse(this.CanBeRuningMarketsCount.Text, out runingMarkets)) e.Cancel = true;
            if (runingMarkets < 0) 
            {
                e.Cancel = true;
                MessageBox.Show("Runing markets count must be greater than zero", "Runing markets count", MessageBoxButtons.OK, MessageBoxIcon.Warning); 
            }
        }
        #region Validating
        private void textBox3_Validating(object sender, CancelEventArgs e)
        {
            double minPer;
            if (!double.TryParse(this.MinPercent.Text, out minPer)) e.Cancel = true;
        }

        private void MaxPercent_Validating(object sender, CancelEventArgs e)
        {
            double maxPer;
            if (!double.TryParse(this.MaxPercent.Text, out maxPer)) e.Cancel = true;
        }

        private void Commission_Validating(object sender, CancelEventArgs e)
        {
            double commission;
            if (!double.TryParse(this.Commission.Text, out commission)) e.Cancel = true;
        }

        private void ABalance_Validating(object sender, CancelEventArgs e)
        {
            double ABalance;
            if (!double.TryParse(this.ABalance.Text, out ABalance) || ABalance < 0) e.Cancel = true;
        }
        #endregion
        #region Validated
        private void ABalance_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.TotalAvailable = double.Parse(this.ABalance.Text);
        }

        private void Commission_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.Comision = double.Parse(this.Commission.Text);
        }

        private void MinPercent_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.MinProfitPercent = double.Parse(this.MinPercent.Text);
        }

        private void MaxPercent_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.MaxProfitPercent = double.Parse(this.MaxPercent.Text);
        }

        private void LimitInMarket_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.MarketRiskLimit = double.Parse(this.LimitInMarket.Text);
        }

        private void CanBeRuningMarketsCount_Validated(object sender, EventArgs e)
        {
            IExchange.MyClass.Client_Cod_Interface.RunigMarketsCount = int.Parse(this.CanBeRuningMarketsCount.Text);
        }
        #endregion
        /*
        private void setSports(List<ListViewItem> sportsList)
        {
            this.listView1.FullRowSelect=true;
            this.listView1.BeginUpdate();
            this.listView1.View = View.Details;
            this.listView1.Columns.Add("Sports");
            this.listView1.Columns.Add("Markets");
            this.listView1.CheckBoxes = true;
            this.listView1.Items.AddRange(sportsList.ToArray());
            this.listView1.AutoResizeColumns(ColumnHeaderAutoResizeStyle.ColumnContent);
            this.listView1.EndUpdate();
        }
        

        private async void GetSports_Click(object sender, EventArgs e)
        {
            Betfair.EventTypeResult[] ptr =await BF.getEventTyps(Betfair.EmptyFiltr);
            List<ListViewItem> rows = new List<ListViewItem>();
            foreach (var el in ptr)
            {
                ListViewItem.ListViewSubItem col2 = new ListViewItem.ListViewSubItem();
                ListViewItem item = new ListViewItem();
                item.Text = el.eventType.name;
                item.Tag = el;
                col2.Text = el.marketCount.ToString();
                item.SubItems.Add(col2);
                rows.Add(item);
            }
            setSports(rows);
        }

        private async void button2_Click(object sender, EventArgs e)
        {
            Betfair.CountryCodeResult[] res = await BF.getCountries(Betfair.EmptyFiltr);
            List<ListViewItem> rows = new List<ListViewItem>();
            ListViewItem row;
            foreach (var el in res)
            {
                row = new ListViewItem();
                row.Text = Betfair.CodToCountry(el.countryCode);
                row.Tag = el;
                ListViewItem.ListViewSubItem col2 =new ListViewItem.ListViewSubItem();
                col2.Text = el.marketCount.ToString();
                row.SubItems.Add(col2);
                rows.Add(row);
            }
            setCountries(rows.ToArray());
        }
        void setCountries(ListViewItem[] countries)
        {
            listView2.Update();
            listView2.CheckBoxes = true;
            listView2.View = View.Details;
            listView2.Columns.Add("Countries");
            listView2.Columns.Add("Count");
            listView2.Items.AddRange(countries);
            listView2.EndUpdate();
        }
        void setCopetitions(ListViewItem[] compet)
        {
            listView3.Update();
            listView3.CheckBoxes = true;
            listView3.View = View.Details;
            listView3.Columns.Add("Name");
            listView3.Columns.Add("Count");
            listView3.Columns.Add("Region");
            listView3.Columns.Add("ID");
            listView3.Items.AddRange(compet);
            listView3.EndUpdate();
        }
        private async void button3_Click(object sender, EventArgs e)
        {
            Betfair.CompetitionResult[] res =await BF.getCompetitions(Betfair.EmptyFiltr);
            List<ListViewItem> rows = new List<ListViewItem>();
            ListViewItem row;
            ListViewItem.ListViewSubItem column;
            foreach (var el in res)
            {
                row = new ListViewItem();
                row.Tag = el;//Column 1
                row.Text = el.competition.name;
                column = new ListViewItem.ListViewSubItem();//Column 2
                column.Text = el.marketCount.ToString();
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//column 3
                column.Text = el.competitionRegion;
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//column 4
                column.Text = el.competition.id;
                row.SubItems.Add(column);
                rows.Add(row);
            }
            setCopetitions(rows.ToArray());
        }

        private async void button4_Click(object sender, EventArgs e)
        {
            Betfair.EventResult[] res =await BF.getEvens(Betfair.EmptyFiltr);
            List<ListViewItem> rows = new List<ListViewItem>();
            ListViewItem row;
            ListViewItem.ListViewSubItem column;
            foreach (var el in res)
            {
                row = new ListViewItem();
                row.Tag = el;//col 1
                row.Text = el.@event.name;
                column = new ListViewItem.ListViewSubItem();//col 2
                column.Text = el.@event.openDate.ToString();
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//col 3
                column.Text = el.@event.venue;
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//col 4
                column.Text = el.@event.id.ToString();
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//col 5
                column.Text = el.@event.timezone;
                row.SubItems.Add(column);
                column = new ListViewItem.ListViewSubItem();//col 6
                column.Text = el.marketCount.ToString();
                row.SubItems.Add(column);
                rows.Add(row);
            }
            setEvens(rows.ToArray());

        }
        void setEvens(ListViewItem[] evens)
        {
            listView4.Update();
            listView4.CheckBoxes = true;
            listView4.View = View.Details;
            listView4.Columns.Add("Name");
            listView4.Columns.Add("Open Date");
            listView4.Columns.Add("Venue");
            listView4.Columns.Add("ID");
            listView4.Columns.Add("Timezone");
            listView4.Columns.Add("Count");
            listView4.Items.AddRange(evens);
            listView4.EndUpdate();
        }

        private async void button5_Click(object sender, EventArgs e)
        {
            Betfair.MarketTypeResult[] res =await BF.getMarketTypes(Betfair.EmptyFiltr);
            List<ListViewItem> rows = new List<ListViewItem>();
            ListViewItem row;
            ListViewItem.ListViewSubItem column;
            foreach (var el in res)
            {
                row = new ListViewItem();
                row.Text = el.marketType;//col 1
                row.Tag = el;
                column = new ListViewItem.ListViewSubItem();
                column.Text = el.marketCount.ToString();
                row.SubItems.Add(column);
                rows.Add(row);
            }
            setMarketType(rows.ToArray());
        }
        public void setMarketType(ListViewItem[] markets)
        {
            listView5.Update();
            listView5.CheckBoxes = true;
            listView5.View = View.Details;
            listView5.Columns.Add("Name");
            listView5.Columns.Add("Count");
            listView5.Items.AddRange(markets);
            listView5.EndUpdate();
        }
        
        private async void button6_Click(object sender, EventArgs e)
        {
            uBF_MP MP = uBF_MP.selectAll();
            Betfair.MarketCatalogue[] res =await BF.getMarketCatalogue(Betfair.EmptyFiltr, MP.getMaxResult(), MP,uBF.MarketSort.FIRST_TO_START);
        }

        private async void button7_Click(object sender, EventArgs e)
        {
            List<string> MarketsID = new List<string>();
            MarketsID.Add("1.101193124");
            MarketsID.Add("1.117098234");
            Betfair.PriceProjection pp = new Betfair.PriceProjection();
            pp.SetPriceData(Betfair.PriceProjection.PriceData.SP_AVAILABLE+Betfair.PriceProjection.PriceData.SP_TRADED);
            Betfair.MarketBook[] resp =await BF.listMarketBook(MarketsID.ToArray(),pp);
        }

        private async void button8_Click(object sender, EventArgs e)
        {
            Betfair.PlaceInstruction pl=new Betfair.PlaceInstruction();
            pl.selectionId=58805L;
            pl.handicap=0;
            pl.side=Betfair.Order.Side.BACK;
            pl.orderType=Betfair.Order.OrderType.LIMIT;
            pl.limitOnCloseOrder=null;
            pl.marketOnCloseOrder=null;
            pl.limitOrder=new Betfair.PlaceInstruction.LimitOrder();
            pl.limitOrder.persistenceType=Betfair.Order.PersistenceType.LAPSE;
            pl.limitOrder.price=4;//KC
            pl.limitOrder.size=2.35;
            List<Betfair.PlaceInstruction> ptr=new List<Betfair.PlaceInstruction>();
            ptr.Add(pl);
            Betfair.PlaceExecutionReport resp = await BF.placeOrders("1.117078491", ptr);
        }

        private async void button9_Click(object sender, EventArgs e)
        {
            Betfair.CancelInstruction inst = new Betfair.CancelInstruction() { betId = "46071443430", sizeReduction = 10 };
            Betfair.CancelInstruction[] ptr=new Betfair.CancelInstruction[]{inst};
            Betfair.CancelExecutionReport resp =await BF.cancelOrders("1.117078630", ptr);
        }

        private async void button10_Click(object sender, EventArgs e)
        {
            Betfair.UpdateInstruction inst = new Betfair.UpdateInstruction();
            inst.betId = "46091642780";
            inst.newPersistenceType = Betfair.UpdateInstruction.PersistenceType.PERSIST;
            Betfair.UpdateInstruction[] ptr=new Betfair.UpdateInstruction[]{inst};
            Betfair.UpdateExecutionReport resp =await BF.updateOrders("1.117140410", ptr);
        }

        private async void button11_Click(object sender, EventArgs e)
        {
            Betfair.ReplaceInstruction inst = new Betfair.ReplaceInstruction();
            inst.betId = "46091834222";
            inst.newPrice = 3;
            Betfair.ReplaceExecutionReport resp = await BF.replaceOrders("1.117140410", (new Betfair.ReplaceInstruction[] { inst }));
        }

        private async void button12_Click(object sender, EventArgs e)
        {
            Betfair.CurrentOrderSummaryReport resp =await BF.listCurrentOrders();
        }

        private async void button13_Click(object sender, EventArgs e)
        {
            Betfair.ClearedOrderSummaryReport resp =await BF.listClearedOrders(Betfair.BetStatus.LAPSED);
        }

        private async void button14_Click(object sender, EventArgs e)
        {
            Betfair.MarketProfitAndLoss[] resp = await BF.listMarketProfitAndLoss(new string[] { "1.117137722" });
        }

        private async void button15_Click(object sender, EventArgs e)
        {
            Betfair.TimeRangeResult[] res =await BF.listTimeRanges(Betfair.EmptyFiltr, Betfair.TimeGranularity.DAYS);
        }

        private async void button16_Click(object sender, EventArgs e)
        {
            Betfair.VenueResult[] resp =await BF.listVenues(Betfair.EmptyFiltr);
        }

        private async void button17_Click(object sender, EventArgs e)
        {
            Betfair.MarketBook[] resp =await Testing_getAllMarkets();
            List<Betfair.MarketBook> bestMarkets = new List<Betfair.MarketBook>();
            double kc1,kc2,kc3,l1,l2,l3;
            double profitValu;
            Dictionary<Betfair.MarketBook, double> ProfitValues = new Dictionary<Betfair.MarketBook,double>();
            foreach (var el in resp)
            {
                //LEVEL_1
                bool valid = true;
                foreach (var runer in el.runners)
                {
                    if (runer.ex.availableToBack.Count == 0 || runer.ex.availableToLay.Count == 0) valid = false;
                }
                if (!valid) continue;
                kc1=el.runners[0].ex.availableToBack[0].price;
                l1=el.runners[0].ex.availableToLay[0].price;
                kc2=el.runners[1].ex.availableToBack[0].price;
                l2=el.runners[1].ex.availableToLay[0].price;
                kc3=el.runners[2].ex.availableToBack[0].price;
                l3=el.runners[2].ex.availableToLay[0].price;

                profitValu=profit(Betfair.nextKcBack(kc1),kc2,kc3);
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }

                profitValu = profit(kc1, Betfair.nextKcBack(kc2), kc3);
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }

                profitValu = profit(kc1, kc2,Betfair.nextKcBack( kc3));
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }

                profitValu = profit(l1, l2, Betfair.nextKcLay(l3),false);
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }

                profitValu = profit(l1, Betfair.nextKcLay(l2), l3, false);
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }

                profitValu = profit(Betfair.nextKcLay(l1), l2, l3, false);
                if (profitValu > 0){ ProfitValues.Add(el,profitValu);  continue; }
            }
            List<KeyValuePair<Betfair.MarketBook, double>> l = new List<KeyValuePair<Betfair.MarketBook, double>>(ProfitValues);
            l.Sort((a, b) => { return a.Value.CompareTo(b.Value); });
        }
        private double profit(double kc1, double kc2, double kc3,bool backOrLay = true)
        {
            if (backOrLay)
            {
                return 100 * ((kc1 * kc2 * kc3) / (kc1 * kc2 + kc2 * kc3 + kc3 * kc1) - 1);
            }
            double temp=kc1*kc2+kc2*kc3+kc3*kc1-kc1*kc2*kc3;
            return 100 * temp / (2 * kc3 * kc2 * kc1 - temp);
        }
        private async Task<Betfair.MarketBook[]> Testing_getAllMarkets()
        {
            string[] marketTypes = new string[] { "MATCH_ODDS" };
            int[] EventTypesId = new int[] { 1 };
            Betfair.MarketFiltr Filtr = new Betfair.MarketFiltr();
            Filtr.AddFiltr(uBF.FiltrNames.eventTypeIds, EventTypesId).AddFiltr(uBF.FiltrNames.marketTypeCodes, marketTypes);
            Betfair.CompetitionResult[] resp = await BF.getCompetitions(Filtr.Filtr);

            List<string> chamIDS = new List<string>();
            foreach (var chamID in resp) { chamIDS.Add(chamID.competition.id); }
            Filtr = new Betfair.MarketFiltr();
            Filtr.AddFiltr(uBF.FiltrNames.marketTypeCodes, marketTypes).AddFiltr(uBF.FiltrNames.competitionIds, chamIDS);
            Betfair.MarketCatalogue[] reqMC = await BF.getMarketCatalogue(Filtr.Filtr, 1000);

            List<Betfair.MarketBook> mb = new List<Betfair.MarketBook>();
            Betfair.PriceProjection FiltrMB = new Betfair.PriceProjection();
            FiltrMB.SetPriceData(Betfair.PriceProjection.PriceData.EX_BEST_OFFERS);

            int threadCount = reqMC.Length / FiltrMB.getMaxMarketIdsCount();
            if (reqMC.Length % FiltrMB.getMaxMarketIdsCount() != 0) threadCount++;

            for (int i = 0; i < threadCount; i++)
            {

                Betfair.MarketCatalogue[] tempMC = new Betfair.MarketCatalogue[FiltrMB.getMaxMarketIdsCount()];
                for (int j = 0; j < FiltrMB.getMaxMarketIdsCount(); j++) { tempMC[j] = reqMC[threadCount * i + j]; }
                List<string> tempStr = new List<string>();
                foreach (var el in tempMC) { tempStr.Add(el.marketId); }
                mb.AddRange(await BF.listMarketBook(tempStr.ToArray(), FiltrMB));
            }
            return mb.ToArray();
        }*/
    }
}
