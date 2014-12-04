///////////////////////////////////////////////////////////////////////////////////////
///  Metadata.cs - Use for generate Metadata with the submitted file                ///
///  ver 1.0                                                                        ///
///                                                                                 ///
///  Language:     Visual C#                                                        ///
///  Platform:     ThinkPad T420, Windows 8                                         ///
///  Author:       Xincheng Lai,            Syracuse Univ.                          /// 
///////////////////////////////////////////////////////////////////////////////////////

/*
 *   Module Operations
 *   -----------------
 *   1.  MetadataTool(string path, List<string> operations) interface to initialize the 
 *   data to store in metadata file
 *   2.  generateMetadata() interface to generate metadata file
 *   3.  updateXMLFile() interface to update metadata file
 *    
 *  
 *   Maintenance History
 *   -------------------  
 *   ver 1.0 : 8 Oct 13
 *     - first release
 * 
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Text;
using System.Threading.Tasks;
 
using System.IO;

namespace Project2
{
    public class MetadataTool
    {
        string m_path, m_name;
        string m_namewithExt = "";
        //string[]  m_args;
        List<string> m_operations;
        string keywordsTowrite = "";
        string categoryTowrite = "";
        string dependencyTowrite = "";
        string descriptionTowrite = "";

        string m_xmlPath = "";

        public MetadataTool(string path, List<string> operations)
        {
            // seperate the name from the path
            m_path = path;
            int pos_end;
            int pos = path.LastIndexOf('\\');
            if( pos == -1)
                pos = path.LastIndexOf('/');
            if( pos > -1) {
                ++pos; 
                m_name = path.Remove(0,pos);
                pos_end = m_name.LastIndexOf('.');
                m_name = m_name.Substring( 0, pos_end );
            } 
            m_namewithExt = Path.GetFileName(path);
            m_operations = operations; 
        }

        public bool generateMetadata( )
        {
            // check the file existed or not

            if ( !File.Exists(m_path) ) 
            {
                Console.WriteLine(" The Input file doesn't exists\n We don't create Metadata!\n ");
                return false;
            }  
            // If the File is existed we create a related Xml file for it
            // m_xmlPath = m_path.Split('.')[0] + m_name + ".xml"; 
            m_xmlPath = m_path.Substring(0, m_path.LastIndexOf("/") +1 )+ m_name + ".xml"; 
            // If the xml file already existed, we update it with exact argument
            if ( File.Exists(m_xmlPath) )
            {
                Console.WriteLine(" The Metadata is already existed!  Updating now ...\n "); 
                createNewXMLFile();
            }
            // If the xml file doesn't exist, we create a new one
            else
            {
                Console.WriteLine("  Creating a new metadata ...\n ");
                createNewXMLFile();
            }
            return true;
        }

        public void updateXMLFile() 
        {
            // load the xml file first
            XDocument doc = XDocument.Load(@m_xmlPath);
            XElement newElem;
            XElement add_pos = doc.Descendants("File").First(); 
            foreach (string item in m_operations )
            { 
                if (item.StartsWith("/K")) {
                    if ( doc.Descendants("KeyWords").Any() != false ){
                        add_pos.Element("KeyWords").Remove();
                    }
                    newElem = new XElement("KeyWords");
                    newElem.Value = item.Substring(2);
                    add_pos.Add(newElem);
                } else if (item.StartsWith("/T")) {
                    if (doc.Descendants("Description").Any() != false)
                    {
                        add_pos.Element("Description").Remove();
                    }
                    newElem = new XElement("Description");
                    newElem.Value = item.Substring(2);
                    add_pos.Add(newElem);
                } else if (item.StartsWith("/D")) {
                    if (doc.Descendants("Dependency").Any() != false)
                    {
                        add_pos.Element("Dependency").Remove();
                    }
                    newElem = new XElement("Dependency");
                    newElem.Value = item.Substring(2);
                    add_pos.Add(newElem);
                }  else if (item.StartsWith("/C")){
                    if (doc.Descendants("Category").Any() != false)
                    {
                        add_pos.Element("Category").Remove();
                    }
                    newElem = new XElement("Category");
                    newElem.Value = item.Substring(2);
                    add_pos.Add(newElem);
                } 
            }
            doc.Save(m_xmlPath + ".xml"); 
            Console.WriteLine(" "+ m_xmlPath + "  Updated!!\n "); 
        }

        void getInfoToWrite()
        {
            foreach (string item in m_operations)
            {
                if (item.StartsWith("/K"))
                {
                    keywordsTowrite += item.Substring(2) + ",";
                }
            }
            foreach (string item in m_operations)
            {
                if (item.StartsWith("/C"))
                {
                    categoryTowrite += item.Substring(2) + ",";
                }
            }
            foreach (string item in m_operations)
            {
                if (item.StartsWith("/D"))
                {
                    dependencyTowrite += item.Substring(2) + ",";
                }
            }  
            foreach (string item in m_operations)
            {
                if (item.StartsWith("/T"))
                {
                    descriptionTowrite += item.Substring(2) ;
                }
            }    
        }

        public void createNewXMLFile() { 
            XDocument xml = new XDocument(
               new XDeclaration("1.0", "utf-8", "yes"),
               new XComment(" This is XML file for " + m_namewithExt),
               new XElement("File")
            );
            XElement newElem;
            XElement add_pos = xml.Descendants("File").First();
            newElem = new XElement("Name");
            newElem.Value = m_namewithExt;
            add_pos.Add(newElem);
            getInfoToWrite();
            newElem = new XElement("Description");
            newElem.Value = descriptionTowrite;
            add_pos.Add(newElem);
            newElem = new XElement("KeyWords");
            newElem.Value = keywordsTowrite;
            add_pos.Add(newElem);
            newElem = new XElement("Category");
            newElem.Value = categoryTowrite;
            add_pos.Add(newElem);
            newElem = new XElement("Dependency");
            newElem.Value = dependencyTowrite;
            add_pos.Add(newElem); 
            xml.Save(m_xmlPath+".xml"); 
            Console.WriteLine("  "+m_xmlPath + "  Created!!\n "); 
            Console.Write("\n\n");
        }

         
        #if(TEST_METADATA)
        [STAThread]
        static void Main(string[] args)
        {
            List<string> mOperations = new List<string>();
            for ( int i = 1; i < args.Length; i++ )
            {
                mOperations.Add( args[i] );
            }

            // Display.showInputArguments(args);

            string fullName = args[0];
            MetadataTool m_test = new MetadataTool(fullName , mOperations);
            m_test.generateMetadata(); 
        } 
        #endif  

    } 
}
