using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IExchange.MyClass
{
    public static class Client_Cod_Interface
    {
        #region Client
            #region Exchange Data
             public static double Comision = -6.5;//Birjayi kamisiyan marketi hamar
             public static double TotalAvailable = 300;//Hasaneli @ndhanur gumar xaxadruyqneri hamar
             public static double MaxProfitPercent = 2;//maximal tokos imitacia stexcelu depqum
             public static double MinProfitPercent = 0.1;//nvazaguyn cankali tokos
             public static double MarketRiskLimit = 20;//aravelaguyn risk@ mek marketi hamar
             public static int RunigMarketsCount = 1;//Tuylatreli marketneri qanak@ xaxadruyq katarelu hamar
            #endregion
            #region Updating Interval
             public static int Interval_GetAllMarkets = 5;//Mintut
             #endregion
        #endregion
        #region Cod
             #region Exceptions
             private static List<Exception> exceptionList;
            public static List<Exception> ExceptionList
            {
                get { return exceptionList; }
                set { exceptionList = value; SetException(value, null); }
            }
            public static event EventHandler SetException;
            #endregion
        #endregion
    }
}
