

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace MatchEngine
{
    class MatchEngine
    {
        const string CfgReadPath = "MatchEngine.cfg";
#if DEBUG
        const string XmlReadPath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
#else
        const string XmlReadPath = "OrderQueue.xml";
#endif

        static StreamReader sr;
        static int AccountAmount;
        static int OrderAmount;
        static int InitialPrice;
        static int TradeRecordStartId;
        static void Main()
        {
            ReadCfg();
            Order[] OrderQueue = new Order[OrderAmount];
            ReadOrderFromXml(ref OrderQueue);
            OrderMatchArray orderMatchArray = new OrderMatchArray(InitialPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            List<TradeRecord> tradeRecord = new List<TradeRecord>();
            for(int i = 0; i < OrderAmount; i++)
            {
                tradeRecord.AddRange(orderMatchArray.AddOrderGetTradeRecord(OrderQueue[i]));
            }
            Console.ReadKey();
        }
        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            AccountAmount = ReadLineFromCfg();
            OrderAmount = ReadLineFromCfg();
            InitialPrice = ReadLineFromCfg();
            TradeRecordStartId = ReadLineFromCfg();
        }

        static int ReadLineFromCfg()
        {
            String line = sr.ReadLine();
            return int.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static void ReadOrderFromXml(ref Order[] OrderQueue)
        {
            OrderQueue = Xml.Deserialize(typeof(Order[]), File.ReadAllText(XmlReadPath)) as Order[];
            Console.WriteLine("OrderQueue.xml loaded successfully.");
        }
    }
}
