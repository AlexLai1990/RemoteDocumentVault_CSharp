///////////////////////////////////////////////////////////////////////
///  Navigate.cs - Navigates a Directory Subtree, displaying files  ///
///  ver 1.2       and some of their properties                     ///
///                                                                 ///
///  Language:     Visual C#                                        ///
///  Platform:     Dell Dimension 8100, Windows Pro 2000, SP2       ///
///  Application:  CSE681 Example                                   ///
///  Author:       Jim Fawcett, CST 2-187, Syracuse Univ.           ///
///                (315) 443-3948, jfawcett@twcny.rr.com            ///
///                
///                 Modified by Xincheng Lai   Syracuse Univ.       ///
///////////////////////////////////////////////////////////////////////
/*
 *  Module Operations:
 *  ==================
 *  Recursively displays the contents of a directory tree
 *  rooted at a specified path, with specified file pattern.
 *  And put the searching results in a list, which can be returned by
 *  fucntion return_my_range()
 *  
 *  
 *
 *  Public Interface:
 *  =================
 *  Navigate nav = new Navigate();
 *  nav.go("c:\temp","*.cs");
 *  public static void printoutFile( string file ) : print out the information of file
 *  nav.go_recursive(string path, string pattern)  : get all files in a folder with specified pattern
 * 
 * 
 *  Maintenance History: 
 *  ====================
 *  ver 1.3 : 8 Oct 13
 *  - add go_recursive() and printoutFile()
 *  ver 1.2 : 10 Sep 11
 *  - removed unnecessary SetCurrentDirectory in Navigate.go()
 *  ver 1.1 : 04 Sep 06
 *  - added file pattern as argument to member function go()
 *  ver 1.0 : 05 Sep 05
 *  - first release
 */

using System;
using System.IO;
using System.Collections.Generic;

namespace Project2
{
  public class Navigate
  {
    List<string> m_search_range = new List<string>();

    public List<string> return_my_range()
    {
        return m_search_range;
    }

    public static void printoutFile( string file )
    {
        string name = Path.GetFileName(file);
        // name = Path.GetFullPath(file);
        FileInfo fi = new FileInfo(file);
        DateTime dt = File.GetLastWriteTime(file);
        Console.Write("   {0,-20} {1,8} bytes  {2}\n", name, fi.Length, dt);
    }

    public void go(string path, string pattern)
    {
        path = Path.GetFullPath(path);
        // check the path exists or not 
        if (!Directory.Exists(path))
        {
            Console.Write("\n  The folder doesn't exist!! Please re-input a path arguments!!\n  ");
            return;
        }
        string [] files = Directory.GetFiles(path, pattern);
        foreach(string file in files)
        {
            m_search_range.Add(file);
        } 
    }

    public void go_recursive(string path, string pattern)
    {
        path = Path.GetFullPath(path); 
        // check the path exists or not 
        if ( !Directory.Exists(path) ) {
            Console.Write("\n  The folder doesn't exist!! Please re-input a path arguments!!\n  ");
            return;
        }

        string[] files = Directory.GetFiles(path, pattern);
        foreach (string file in files)
        {
            m_search_range.Add( file );
        } 

        string[] dirs = Directory.GetDirectories(path);
        foreach (string dir in dirs)
        {
            go_recursive(dir, pattern); 
        }
    }


#if(TEST_NAVIGATE)
    [STAThread]
    static void Main(string[] args)
    {
      Console.Write("\n  Demonstrate Navigate Package ");
      Console.Write("\n ==============================");

      string path;
      if (args.Length > 0)
        path = args[0];
      else
        path = Directory.GetCurrentDirectory();

      Navigate m_nave = new Navigate();
      m_nave.go(path, "*.*"); 

      Console.Write("\n\n  Include files: ");
      Console.Write("\n\n  ");
      foreach (var item in m_nave.m_search_range)
          Console.Write(item + "\n  ");
      Console.Write("\n\n");


      Navigate m_nave_cur = new Navigate();
      m_nave_cur.go_recursive(path, "*.*");

      Console.Write("\n\n  Recursive files: ");
      Console.Write("\n\n  ");
      foreach (var item in m_nave_cur.m_search_range)
          Console.Write(item + "\n  ");
      Console.Write("\n\n");

    }
#endif
  }
}
