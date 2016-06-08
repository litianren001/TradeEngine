

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
        const string XmlWritePath= "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/TradeRecord.xml";
#else
        const string XmlReadPath = "OrderQueue.xml";
        const string XmlWritePath= "TradeRecord.xml";
#endif


        static StreamReader sr;
        static int IsFileImplementation;
        static int IsMessageQueueImplementation;
        static int IsWebImplementation;
        static int AccountAmount;
        static int OrderAmount;
        static int InitialPrice;
        static int TradeRecordStartId;

        static void Main()
        {
            ReadCfg();
            if (IsFileImplementation == 1)
                FileImplementation();
            else if (IsMessageQueueImplementation == 1)
                MessageQueueImplementation();
            else if (IsWebImplementation == 1)
                WebImplementation();
            Console.ReadKey();
        }

        static void FileImplementation()
        {
            Order[] OrderQueue = new Order[OrderAmount];
            ReadOrderFromXml(ref OrderQueue);
            OrderMatchList orderMatchList = new OrderMatchList(InitialPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            List<TradeRecord> tradeRecord = new List<TradeRecord>();
            for (int i = 0; i < OrderAmount; i++)
            {
                tradeRecord.AddRange(orderMatchList.AddOrderGetTradeRecord(OrderQueue[i]));
            }
            WriteTradeRecordToXml(ref tradeRecord);
        }

        static void MessageQueueImplementation()
        {

        }

        static void WebImplementation()
        {

        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            IsFileImplementation = ReadLineFromCfg();
            IsMessageQueueImplementation = ReadLineFromCfg();
            IsWebImplementation = ReadLineFromCfg();
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

        static void ReadOrderFromXml(ref Order[] orderQueue)
        {
            orderQueue = Xml.Deserialize(typeof(Order[]), File.ReadAllText(XmlReadPath)) as Order[];
            Console.WriteLine("OrderQueue.xml loaded successfully.");
        }

        static void WriteTradeRecordToXml(ref List<TradeRecord> tradeRecord)
        {
            File.WriteAllText(XmlWritePath, Xml.XMLSerializer(typeof(List<TradeRecord>), tradeRecord));
            Console.WriteLine("TradeRecord.xml created successfully.");
        }
    }
}
