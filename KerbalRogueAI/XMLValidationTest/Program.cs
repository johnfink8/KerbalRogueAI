using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using KerbalRogueAI;
using System.Xml;
using System.IO;

namespace XMLValidationTest
{
    class Program
    {
        static void Main(string[] args)
        {
            AICore aicore = new AICore();
            AIXmlParser parser = new AIXmlParser(aicore);
            XmlNode node=null;
            Directory.SetCurrentDirectory("C:\\Program Files (x86)\\Steam\\steamapps\\common\\Kerbal Space Program");
            foreach (string filename in Directory.GetFiles("GameData\\RogueAI\\FlightPlans"))
            {
                if (filename.ToLower().EndsWith(".xml"))
                {
                    try
                    {
                        if (!parser.LoadDocument(
                            filename,
                            out node))
                            Console.WriteLine("Failed Validate");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.ToString());
                    }
                    if (node == null)
                        Console.WriteLine("Invalid XML file");
                    Console.WriteLine("Validated " + filename);
                    parser.GetConditions(filename);
                    parser.xmldocument = null;
                    var op = new AIOperationWarp(aicore);
                    Console.WriteLine(op.TimeTranslate(993456));
                }
            }
        }
    }
}
