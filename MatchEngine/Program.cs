

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.Data;

namespace MatchEngine
{
    class Program
    {
        static string CfgReadPath = "MatchEngine.cfg";
        static StreamReader sr;
        static int AccountAmount;
        static int OrderAmount;
        static int MinPrice;
        static int MaxPrice;
        //static string XmlReadPath = "OrderQueue.xml";
        static string XmlReadPath = "C:/Users/litia_000/Documents/Visual Studio 2015/Projects/TradeEngine/OrderQueue.xml";
        static void Main()
        {
            ReadCfg();
            Order[] OrderQueue = new Order[OrderAmount];
            ReadOrderFromXml(ref OrderQueue);
            Order[][] PriceHashTable=new Order[MaxPrice-MinPrice][];

            Console.ReadKey();
        }
        static void ReadCfg()
        {
            sr = new StreamReader(CfgReadPath);
            AccountAmount = ReadLineFromCfg();
            OrderAmount = ReadLineFromCfg();
            MinPrice = ReadLineFromCfg();
            MaxPrice = ReadLineFromCfg();
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
