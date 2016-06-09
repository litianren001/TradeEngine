

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Timers;
using System.Messaging;

namespace MatchEngine
{
    class MatchEngine
    {
        const string CfgReadPath = "MatchEngine.cfg";
#if DEBUG
        const string XmlReadPath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
        const string XmlWritePath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/TradeRecord.xml";
#else
        const string XmlReadPath = "OrderQueue.xml";
        const string XmlWritePath= "TradeRecord.xml";
#endif

        const string MessageQueuePath = @".\Private$\OrderQueue";
        const string MessageQueueName = "OrderQueue";

        static StreamReader sr;
        static int IsFileImplementation;
        static int IsMessageQueueImplementation;
        static int IsWebImplementation;
        static int AccountAmount;
        static int OrderAmount;
        static int InitialPrice;
        static int TradeRecordStartId;
        static int MessageReceiveInterval;

        static OrderMatchList OrderMatchList;
        static List<TradeRecord> TradeRecordList;
        static MessageQueue OrderQueue;

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
            OrderMatchList = new OrderMatchList(InitialPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            TradeRecordList = new List<TradeRecord>();
            for (int i = 0; i < OrderAmount; i++)
            {
                TradeRecordList.AddRange(OrderMatchList.AddOrderGetTradeRecord(OrderQueue[i]));
            }
            WriteTradeRecordToXml(ref TradeRecordList);
        }

        static void MessageQueueImplementation()
        {
            OrderMatchList = new OrderMatchList(InitialPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            TradeRecordList = new List<TradeRecord>();

            if (MessageQueue.Exists(MessageQueuePath))
            {
                OrderQueue = new MessageQueue(MessageQueuePath);
            }
            else
            {
                Console.WriteLine("Message Queue isn't exist.");
                return;
            }
            System.Type[] types = new Type[1];
            types[0] = typeof(Order);
            OrderQueue.Formatter = new XmlMessageFormatter(types);
            Timer timer = new Timer(MessageReceiveInterval);
            timer.Elapsed += new ElapsedEventHandler(ReceiveOrderFromMessageQueue);
            timer.AutoReset = true;
            timer.Enabled = true;

        }

        static void ReceiveOrderFromMessageQueue(Object source, ElapsedEventArgs e)
        {
            Order newOrder = (OrderQueue.Receive().Body) as Order;
            TradeRecordList.AddRange(PrintTradeRecord(OrderMatchList.AddOrderGetTradeRecord(newOrder)));

        }

        static List<TradeRecord> PrintTradeRecord(List<TradeRecord> tradeRecord)
        {
            foreach (var e in tradeRecord)
            {
                Console.WriteLine($"Record #{e.Uid}:\tBuyer:{e.BuyerUid}\tSeller:{e.SellerUid}\tAmount:{e.Amount}\tPrice:{e.Price}");
            }
            return tradeRecord;
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
            MessageReceiveInterval = ReadLineFromCfg();
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
