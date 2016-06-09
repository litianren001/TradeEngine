

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;
using System.Timers;
using System.Messaging;
using System.Net;
using System.Net.Sockets;

namespace MatchEngine
{
    class MatchEngine
    {
        const string CfgReadPath = "MatchEngine.cfg";
#if DEBUG
        const string FileReadPath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
        const string FileWritePath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/TradeRecord.xml";
#else
        const string FileReadPath = "OrderQueue.xml";
        const string FileWritePath = "TradeRecord.xml";
#endif

        const string OrderMessageQueuePath = @".\Private$\TLIOrderQueue";
        const string OrderMessageQueueName = "OrderQueue";
        const int OrderMessageQueueJournalSize = 1000;

        const string PriceMessageQueuePath = @".\Private$\TLIPriceQueue";
        const string PriceMessageQueueName = "PriceQueue";
        const int PriceMessageQueueJournalSize = 1000;

        static StreamReader sr;
        static int IsFileImplementation;
        static int IsMessageQueueImplementation;
        static int IsWebImplementation;
        static int AccountAmount;
        static int CurrentPrice;
        static int TradeRecordStartId;
        static int FileOrderAmount;
        static int MessageQueueOrderReceiveInterval;
        static string WebHostMatchEngineIp;
        static int WebHostMatchEnginePort;


        static OrderMatchList OrderMatchList;
        static List<TradeRecord> TradeRecordList;
        static MessageQueue OrderQueue;
        static MessageQueue PriceQueue;

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
            Order[] OrderQueue = new Order[FileOrderAmount];
            ReadOrderFromXml(ref OrderQueue);

            OrderMatchList = new OrderMatchList(CurrentPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            TradeRecordList = new List<TradeRecord>();

            for (int i = 0; i < FileOrderAmount; i++)
            {
                TradeRecordList.AddRange(OrderMatchList.AddOrderGetTradeRecord(OrderQueue[i]));
            }
            WriteTradeRecordToXml(ref TradeRecordList);
        }

        static void MessageQueueImplementation()
        {
            OrderMatchList = new OrderMatchList(CurrentPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            TradeRecordList = new List<TradeRecord>();

            if (MessageQueue.Exists(OrderMessageQueuePath))
            {
                OrderQueue = new MessageQueue(OrderMessageQueuePath);
            }
            else
            {
                OrderQueue = MessageQueue.Create(OrderMessageQueuePath);
                OrderQueue.Label = OrderMessageQueueName;
                OrderQueue.UseJournalQueue = true;
                OrderQueue.MaximumJournalSize = OrderMessageQueueJournalSize;
            }
            if (MessageQueue.Exists(PriceMessageQueuePath))
            {
                PriceQueue = new MessageQueue(PriceMessageQueuePath);
            }
            else
            {
                PriceQueue = MessageQueue.Create(PriceMessageQueuePath);
                PriceQueue.Label = PriceMessageQueueName;
                PriceQueue.UseJournalQueue = true;
                PriceQueue.MaximumJournalSize = PriceMessageQueueJournalSize;
            }

            OrderQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(Order) });
            PriceQueue.Formatter = new XmlMessageFormatter(new Type[] { typeof(int) });
            Timer timer = new Timer(MessageQueueOrderReceiveInterval);
            timer.Elapsed += new ElapsedEventHandler(ReceiveOrderFromMessageQueue);
            timer.AutoReset = true;
            timer.Enabled = true;

        }

        static void ReceiveOrderFromMessageQueue(Object source, ElapsedEventArgs e)
        {
            Order newOrder = (OrderQueue.Receive().Body) as Order;
            TradeRecordList.AddRange(PrintTradeRecord(OrderMatchList.AddOrderGetTradeRecord(newOrder)));
            if (CurrentPrice != OrderMatchList.CurrentPrice)
            {
                CurrentPrice = OrderMatchList.CurrentPrice;
                PriceQueue.Send(OrderMatchList.CurrentPrice);
            }
        }

        static List<TradeRecord> PrintTradeRecord(List<TradeRecord> tradeRecord)
        {
            foreach (var e in tradeRecord)
            {
                Console.WriteLine($"Record #{e.Uid}:\tBuyer:{e.BuyerUid}\tSeller:{e.SellerUid}\tAmount:{e.Amount}\tPrice:{e.Price}");
            }
            return tradeRecord;
        }

        static bool IsQueueEmpty(string path)
        {
            bool isQueueEmpty = false;
            var myQueue = new MessageQueue(path);
            try
            {
                myQueue.Peek(new TimeSpan(0));
                isQueueEmpty = false;
            }
            catch (MessageQueueException e)
            {
                if (e.MessageQueueErrorCode == MessageQueueErrorCode.IOTimeout)
                {
                    isQueueEmpty = true;
                }
            }
            return isQueueEmpty;
        }

        static void WebImplementation()
        {
            OrderMatchList = new OrderMatchList(CurrentPrice);
            TradeRecord.SetTradeRecordStartId(TradeRecordStartId);
            TradeRecordList = new List<TradeRecord>();

            IPAddress hostIp = Dns.GetHostAddresses(WebHostMatchEngineIp)[0];
            TcpListener listener = new TcpListener(hostIp, WebHostMatchEnginePort);
            listener.Start();
            Console.WriteLine("Waiting for connection...");
            while (true)
            {
                TcpClient client = listener.AcceptTcpClient();
                NetworkStream ns = client.GetStream();

                byte[] byteNewOrder = new byte[1024];
                int byteNewOrderRealLength = ns.Read(byteNewOrder, 0, byteNewOrder.Length);
                Order newOrder = Xml.Deserialize(typeof(Order),Encoding.UTF8.GetString(byteNewOrder, 0, byteNewOrderRealLength)) as Order;
                Console.WriteLine("Order is received and latest price is sent.");
                TradeRecordList.AddRange(PrintTradeRecord(OrderMatchList.AddOrderGetTradeRecord(newOrder)));

                CurrentPrice = OrderMatchList.CurrentPrice;
                byte[] bytePrice = Encoding.ASCII.GetBytes(CurrentPrice.ToString());
                ns.Write(bytePrice, 0, bytePrice.Length);
                client.Close();

            }
        }

        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            IsFileImplementation = ReadIntFromCfg();
            IsMessageQueueImplementation = ReadIntFromCfg();
            IsWebImplementation = ReadIntFromCfg();
            AccountAmount = ReadIntFromCfg();
            CurrentPrice = ReadIntFromCfg();
            TradeRecordStartId = ReadIntFromCfg();
            FileOrderAmount = ReadIntFromCfg();
            MessageQueueOrderReceiveInterval = ReadIntFromCfg();
            WebHostMatchEngineIp = ReadStringFromCfg();
            WebHostMatchEnginePort = ReadIntFromCfg();

        }

        static int ReadIntFromCfg()
        {
            String line = sr.ReadLine();
            return int.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static double ReadDoubleFromCfg()
        {
            String line = sr.ReadLine();
            return double.Parse(line.Substring(line.IndexOf('=') + 1));
        }

        static string ReadStringFromCfg()
        {
            String line = sr.ReadLine();
            return line.Substring(line.IndexOf('=') + 1);
        }

        static void ReadOrderFromXml(ref Order[] orderQueue)
        {
            orderQueue = Xml.Deserialize(typeof(Order[]), File.ReadAllText(FileReadPath)) as Order[];
            Console.WriteLine("OrderQueue.xml loaded successfully.");
        }

        static void WriteTradeRecordToXml(ref List<TradeRecord> tradeRecord)
        {
            File.WriteAllText(FileWritePath, Xml.XMLSerializer(typeof(List<TradeRecord>), tradeRecord));
            Console.WriteLine("TradeRecord.xml created successfully.");
        }
    }
}
