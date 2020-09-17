using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using System.Xml.XPath;

namespace WpfApp1
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public static DataTable dt = new DataTable();

        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //excecution time start
            

            OpenFileDialog openFileDialog = new OpenFileDialog();
            string path;
            if (openFileDialog.ShowDialog() == true)
            {
                var watch = new System.Diagnostics.Stopwatch();
                watch.Start();

                //txtEditor.Text = File.ReadAllText(openFileDialog.FileName);
                path = openFileDialog.FileName;
                               
                //string path = @"C:\Users\\Downloads\TFP-64-24M-FGLID.rfa";
                //Extract AtomXml from Rfa
                string atomXml = ExtractAtomXmlFromRfa(path);
                //Console.WriteLine(atomXml);

                //initialize XML document class from string
                XmlDocument xmlDoc = new XmlDocument();
                //load XML
                xmlDoc.LoadXml(atomXml);
                //rfa <entry> element
                XmlNode root = xmlDoc.DocumentElement;

                //Display the contents of the child nodes.
                PrintXmlNodes(xmlDoc);
                
                txtbox1.DataContext = root.InnerXml;
                //txtbox1.Text = path.ToString();
                filePath.Text = path.ToString();

                DataSet dataSet = new DataSet();
                XmlTextReader xtr = new XmlTextReader(root.LastChild.LastChild.OuterXml, XmlNodeType.Element, null);
                //XmlNodeReader xread =
                dataSet.ReadXml(xtr);
                DataView dataView = new DataView(dataSet.Tables[2]);
                //dataGrid1.ItemsSource = dataView;
                


                XmlNamespaceManager namespaceManager = new XmlNamespaceManager(xmlDoc.NameTable);
               
                namespaceManager.AddNamespace("atom", "http://www.w3.org/2005/Atom");                
                namespaceManager.AddNamespace("A", "urn:schemas-autodesk-com:partatom");
          
                XmlNode familyNameNode = xmlDoc.SelectSingleNode("atom:entry/atom:title", namespaceManager); 
                familyName.Text = familyNameNode.InnerText;
                XmlNode familyCategoryNode = xmlDoc.SelectSingleNode("atom:entry/atom:category/atom:term", namespaceManager);
                familyCategory.Text = familyCategoryNode.InnerText;
                XmlNode familyProdVersionNode = xmlDoc.SelectSingleNode("atom:entry/atom:link/A:design-file/A:product-version", namespaceManager);
                familyProdVersion.Text = familyProdVersionNode.InnerText;
                XmlNode familyUpdatedDateNode = xmlDoc.SelectSingleNode("atom:entry/atom:link/A:design-file/A:updated", namespaceManager);
                familyUpdatedDate.Text = familyUpdatedDateNode.InnerText;

                XmlNode familyFeaturesNode = xmlDoc.SelectSingleNode("atom:entry/A:features/A:feature", namespaceManager);

                
                DataColumn paramName = new DataColumn("parameter");
                DataColumn paramValue = new DataColumn("value");
                dt.Columns.Add(paramName);
                dt.Columns.Add(paramValue);

                NodeToTable(familyFeaturesNode);
                dataGrid1.ItemsSource = dt.DefaultView;
                //excecution time stop
                watch.Stop();
                Console.WriteLine($"Execution Time: {watch.ElapsedMilliseconds} ms");

                //Console.ReadKey();
            }



        }


        /// <summary>
        /// Reads line by line and Extracts AtomXML from revit family file
        /// </summary>
        /// <param name="path"></param>
        /// <returns>string</returns>
        static string ExtractAtomXmlFromRfa(string path)
        {
            //define variables
            string startString = "<?xml";
            string endString = "</entry>";
            string chunks = "";
            string xml = "";
            bool startStringFound = false;
            bool endStringFound = false;

            StreamReader streamReader = File.OpenText(path);

            while ((!startStringFound || !endStringFound) && !streamReader.EndOfStream)
            {
                string line = streamReader.ReadLine();
                Byte[] lineByte = Encoding.ASCII.GetBytes(line);
                string lineEncoded = Encoding.ASCII.GetString(lineByte);
                //Console.WriteLine(lineEncoded);
                if (lineEncoded.Contains(startString))
                {
                    //Console.WriteLine("start string {0} found in the following line:\n{1}", startString, lineEncoded);
                    startStringFound = true;
                }
                if (startStringFound)
                {
                    chunks += lineEncoded;
                    if (lineEncoded.Contains(endString))
                    {
                        //Console.WriteLine("end string {0} found in the following line:\n{1}", endString, lineEncoded);
                        int startIndex = chunks.IndexOf(startString);
                        int endIndex = chunks.IndexOf(endString);
                        xml = chunks.Substring(startIndex, endIndex + endString.Length - startIndex);
                        //Console.WriteLine(xml);
                        endStringFound = true;
                    }
                }
            }
            //XmlDocument xmlDoc = new XmlDocument();
            //xmlDoc.LoadXml(xml);
            return xml;
        }

        /// <summary>
        /// Prints in console Xml elements and Inner text
        /// </summary>
        /// <param name="node"></param>
        static void PrintXmlNodes(XmlNode node)
        {
            if (node.HasChildNodes)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    XmlNode inode = node.ChildNodes[i];
                    if (node.ChildNodes[i].FirstChild != null)
                    {
                        if (node.ChildNodes[i].NodeType == XmlNodeType.Element && node.ChildNodes[i].FirstChild.NodeType == XmlNodeType.Element)
                        {
                            int parentCount = NodeParentCount(node.ChildNodes[i], 0);
                            Console.WriteLine(new String('=', parentCount) + " " + node.ChildNodes[i].LocalName);
                            PrintXmlNodes(node.ChildNodes[i]);
                        }
                        if (node.ChildNodes[i].NodeType == XmlNodeType.Element && node.ChildNodes[i].FirstChild.NodeType == XmlNodeType.Text)
                        {
                            int parentCount = NodeParentCount(node.ChildNodes[i], 0);
                            Console.WriteLine(new String('=', parentCount) + " " + node.ChildNodes[i].LocalName + " : " + node.ChildNodes[i].InnerText);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("ELSE");
            }
        }

        static DataTable NodeToTable(XmlNode node)
        {         


            if (node.HasChildNodes)
            {
                for (int i = 0; i < node.ChildNodes.Count; i++)
                {
                    XmlNode inode = node.ChildNodes[i];
                    if (node.ChildNodes[i].FirstChild != null)
                    {
                        if (node.ChildNodes[i].NodeType == XmlNodeType.Element && node.ChildNodes[i].FirstChild.NodeType == XmlNodeType.Element)
                        {
                            //if (node.ChildNodes[i].Name == "A:group")
                            //{
                            //    Console.WriteLine(node.ChildNodes[i].FirstChild.InnerText);
                            //}
                            int parentCount = NodeParentCount(node.ChildNodes[i], 0);                            
                            Console.WriteLine(new String('=', parentCount) + " " + node.ChildNodes[i].LocalName);
                            //add rows
                            //DataRow row = dt.NewRow();
                            //row[0] = node.ChildNodes[i].LocalName;
                            //dt.Rows.Add(row);
                            //recursivity
                            NodeToTable(node.ChildNodes[i]);                                                 

                        }
                        if (node.ChildNodes[i].NodeType == XmlNodeType.Element && node.ChildNodes[i].FirstChild.NodeType == XmlNodeType.Text)
                        {
                            int parentCount = NodeParentCount(node.ChildNodes[i], 0);
                            Console.WriteLine(new String('=', parentCount) + " " + node.ChildNodes[i].LocalName + " : " + node.ChildNodes[i].InnerText);
                            //
                            DataRow row = dt.NewRow();
                            row[0] = node.ChildNodes[i].LocalName;
                            row[1] = node.ChildNodes[i].InnerText;
                            dt.Rows.Add(row);
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine("ELSE");
            }
            return dt;
        }

        /// <summary>
        /// Counts the number of parents that a XML node is nested in
        /// </summary>
        /// <param name="node">XmlNode</param>
        /// <param name="count">int initial counter. Usually, 0</param>
        /// <returns>int</returns>
        static int NodeParentCount(XmlNode node, int count)
        {
            if (node.ParentNode != null)
            {
                count++;
                return NodeParentCount(node.ParentNode, count);
            }
            else
            {
                return count;
            }
        }
    

    }
}
