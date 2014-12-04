///////////////////////////////////////////////////////////////////////////////////////
///  TextSearch.cs - Use for search the file based on the input arguments           ///
///  ver 1.0                                                                        ///
///                                                                                 ///
///  Language:     Visual C#                                                        ///
///  Platform:     ThinkPad T420, Windows 8                                         ///
///  Author:       Xincheng Lai,            Syracuse Univ.                          /// 
///////////////////////////////////////////////////////////////////////////////////////

/*
 *   Module Operations
 *   -----------------
 *   This module is used for showing the instructions and results on the console,
 *   it is convenient for using CTA and geting the idea about the result.
 *   
 *   Public Interface
 *   ----------------  
 *   m_textSearch.searhText() ： Due to the searching range, do the searching function to find the matching file
 *   return_my_search_result() ： Return the finding results
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
using System.Text;
using System.Threading.Tasks;

using Microsoft.Office.Interop.Word; 

using System.IO;

namespace Project2
{
    public class TextSearch
    {
        string m_pattern, m_path; 
        string m_operation = "/A";
        List<string> m_searching_elements;
        bool m_Rflag = false;

        Navigate m_navigate;
        List<string> m_searching_range;
        List<string> m_search_result;
        

        public TextSearch(string pattern, string path, bool flag_recursive, List<string> searching_elements, string operation) {
            m_pattern = pattern;
            m_path = path;
            m_Rflag = flag_recursive;
            m_searching_elements = searching_elements;
            m_operation = operation;
            m_navigate = new Navigate();
            if (m_Rflag){
                m_navigate.go_recursive(path, pattern);
            }
            else{
                m_navigate.go(path, pattern);
            }
            m_searching_range = m_navigate.return_my_range();


         //   Display.showTitle("The Searching range according to the input arguments is the following files: ");
         //   Display.searchRange(m_searching_range);

        }

        public List<string> return_my_search_result()
        {
            return m_search_result;
        }

        public bool searhText() {
            List<string> search_result = new List<string>();
            checkSearchRange();
            foreach (var file in m_searching_range) 
            {
                bool check_pass = false ;
                foreach (var m_search in m_searching_elements)
                    if ( searchWordFile (file,m_search) || coreSearchTextInFile(file, m_search) ) {
                        check_pass = true;
                        if (m_operation == "/O")
                            break;
                    }
                    else{
                        check_pass = false;
                        if (m_operation == "/O") 
                            continue;
                        break;
                    } 
                if (check_pass == true)
                    search_result.Add(file);
            }
            m_search_result = search_result;
            return true;
        }

        public void checkSearchRange() 
        {
            if (m_searching_elements.Count == 0)
            {
                Console.Write("\n  The searching elements cannot be empty!! \n "); 
            }
        }


        public bool coreSearchTextInFile( string fileName, string text ) 
        {
            StreamReader streamReader = new StreamReader(fileName);
            string mContaintext = streamReader.ReadToEnd();
            streamReader.Close();
            if ( mContaintext.Contains(text) )
                return true;
            else return false;
        }

        public bool searchWordFile(string fileName, string text) 
        { 
            if (!(System.IO.Path.GetExtension(fileName) == ".doc" || System.IO.Path.GetExtension(fileName) == ".docx"))
                return false;
            List<string> m_contains = new List<string>();
            object file = fileName;
            object isReadOnly = false;
            object isVisible = true;
            object isMissing = System.Reflection.Missing.Value;

            Microsoft.Office.Interop.Word.Application WordApp = new Microsoft.Office.Interop.Word.Application();
            Microsoft.Office.Interop.Word.Document WordDoc = WordApp.Documents.Open(ref file,
                            ref isMissing, ref isReadOnly,
                            ref isMissing, ref isMissing, ref isMissing,
                            ref isMissing, ref isMissing, ref isMissing,
                            ref isMissing, ref isMissing, ref isVisible,
                            ref isMissing, ref isMissing, ref isMissing);

            for (int i = 0; i < WordDoc.Paragraphs.Count; i++)
            {
                m_contains.Add(WordDoc.Paragraphs[i + 1].Range.Text.ToString());
            }

            var m_searhResult = m_contains.FindAll(i => i.Contains(text));

            ( (Microsoft.Office.Interop.Word._Document)WordDoc ).Close();
            ( (Microsoft.Office.Interop.Word._Application)WordApp ).Quit();

            if ( m_searhResult.Count > 0 )
                return true;
            else
                return false; 
        }


        #if(TEST_TEXTSEARCH)
        [STAThread]
        // FORMAT *.txt ../../TestFiles "/Tthread demo" /Mname /R /A (/O)
        static void Main(string[] args)
        {
            List<string> mOperations = new List<string>();
            for (int i = 0; i < args.Length; i++)
            {
                mOperations.Add(args[i]);
            }
             
         //   Display.showInputArguments(args);

            List<string> mSearch = new List<string>();
            bool flag_r = false;
            string operation = "/A";
            foreach (var s in mOperations)
            { 
                if ( s.StartsWith("/T") )
                    mSearch.Add( s.Substring(2));
                else if ( s.StartsWith("/R") )
                    flag_r = true;
                else if ( s.StartsWith("/A") )
                    operation = "/A";
                else if ( s.StartsWith("/O") )
                    operation = "/O";
            }
            
            // 1. Check the range of search 
            //  Console.Write("\n\n  The Searching range according to the input argument is the following files: \n  ");
            // in Initialization, we get the searching range.
            TextSearch m_textSearch = new TextSearch(args[0], args[1],flag_r, mSearch, operation);
            m_textSearch.searhText();
            List<string> mRes = m_textSearch.return_my_search_result();
            Console.Write( "\n\n  The file contains elements: ");
            foreach (var item in mSearch)
                Console.Write( item + "  ");
            Console.Write( "\n\n  Following are the list of found file: \n  ");
            foreach (var item in mRes) 
            {
                Console.Write(item + "\n  ");
            }
        }
        #endif
    }
}
