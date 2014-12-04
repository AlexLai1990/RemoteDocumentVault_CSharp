/////////////////////////////////////////////////////////////////////
// Server.cs -  RemoteServer to provide services                   //
// ver 1.0                                                         //
// Author: Xincheng Lai Based on Jim Fawcett                       // 
/////////////////////////////////////////////////////////////////////
/*
 * Note: 
 * This server contains the functionalities to control the dispatch of all
 * messages to each service
 * 
 * I hard code the ServerUrl = "http://localhost:8001/ICommunicator/Server".
 * The client's IP address can be automatically generate by their side,
 * When they send message to server, those IP, port soucre infomation will
 * also be pass to the server.
 * 
 * 
 * 
 * Maintenance History:
 * ====================
 * ver 1.1 : 12/15/2013
 * ver 1.0 : 11/5/2013
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCF_Comm;
using System.Threading.Tasks;
using System.Threading;
using System.Collections;
using Relationship;

using System.IO;

using Project2;

namespace Server
{
    public class Server
    {
        private Receiver receiver = null;
        private Sender sndr;
        private Thread rcvThrd = null;
        private const string ServerUrl = "http://localhost:8001/ICommunicator/Server"; //
        // the public message is updated each time when the server receives
        private Message recvrMsg = null;
        private RelationshipBuilder m_relationship = null;

        private string m_fileConent = "";
        private string m_fileMetadata = "";

        private ArrayList m_textqueryres;
        private ArrayList m_textqueryfiles;

        private ArrayList m_metadataqueryres;

        //--------------- Constructor will building a map according to current categories and files in the server --------------
        public Server()
        {
            m_relationship = new RelationshipBuilder("../../../Repository");
        }

        //-------------- Deconstruction will close the sender and receiver --------------
        ~Server()
        {
            sndr.Close();
            receiver.Close();
        }

        //-------------- Running the server after the Server is initialized --------------
        public void Run()
        {
            try
            {
                receiver = new Receiver();
                receiver.CreateRecvChannel(ServerUrl);

                rcvThrd = new Thread(new ThreadStart(this.recThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
                // wait for it finished
                rcvThrd.Join();
            }
            catch (Exception ex)
            {
                StringBuilder msg = new StringBuilder(ex.Message);
                Console.WriteLine(msg);
            }
        }

        //-------------- each time the server get a message, it will be passed to according service --------------
        private void recThreadProc()
        {
            while (true)
            {
                recvrMsg = receiver.GetMessage();
                MessageDispatch(recvrMsg);
                Console.WriteLine("\n  The server received message : " + recvrMsg.clickName + "\n  ");
            }
        }

        // -------------- Dispatch messages accroding to its tags and do the corresponding service --------------
        void MessageDispatch(Message msg)
        {
            try
            {
                switch (msg.Tag)
                {
                    case MessageTag.CATEGORY:
                        GoCategory(msg);
                        break;
                    case MessageTag.UPLOAD:
                        GoUpload(msg);
                        break;
                    case MessageTag.SHOWFILE:
                        GoShowFile(msg);
                        break;
                    case MessageTag.EDITMETADATA:
                        GoEditView(msg);
                        break;
                    case MessageTag.TEXTQUERY:
                        GoTextQueryService(msg);
                        break;
                    case MessageTag.METADATAQUERY:
                        GoMetadataQueryService(msg);
                        break;
                    case MessageTag.UPDATE:
                        GoRebuildMap(msg);
                        break;
                }
            }
            catch
            {
                Console.WriteLine("\n  Cannot finding the correct message! \n  ");
            }
        }

        // -------------- This is used for server to send back message to client --------------
        private void SendBackMessage(Message message, string IP, string port)
        {
            Message msg = new Message();
            try
            {
                string clientEndpoint = "http://" + IP + ":" + port + "/ICommunicator/Client";
                // sending each message will create a new sender class and with a end tag to finish sending
                sndr = new Sender(clientEndpoint);
                sndr.PostMessage(message);
                msg.Tag = MessageTag.END;
                sndr.PostMessage(msg);
            }
            catch
            {
                Console.WriteLine("\n  Sending back messages failed!! \n  ");
            }
        }

        // ------------------------------- Service based on different client view ------------------------------ 
        // -------------- Service on Category view --------------
        void GoCategory(Message msg)
        {
            switch (msg.clickName)
            {
                case "CategoryView":
                    Console.WriteLine("\n  IN category service \n  ");
                    Message replyMSG = new Message();
                    replyMSG.Tag = MessageTag.CATEGORY;
                    replyMSG.clickName = "CategoryList";
                    replyMSG.contents = new ArrayList(m_relationship.Get_MapOfCategories.Keys);
                    SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);
                    break;
                case "ClickCategoryListBox":
                    Message replyMSG_1 = new Message();
                    replyMSG_1.Tag = MessageTag.CATEGORY;
                    replyMSG_1.clickName = "FilesListBoxChanged";
                    replyMSG_1.contents = (ArrayList)m_relationship.Get_MapOfCategories[msg.item];
                    SendBackMessage(replyMSG_1, msg.srcIP, msg.srcPort);
                    break;
                case "ClickFilesListBox":
                    if (msg.item != "")
                    {
                        if (m_relationship.Get_MapOfFiles.ContainsKey(msg.item))
                        {
                            Message replyMSG_2 = new Message();
                            replyMSG_2.Tag = MessageTag.CATEGORY;
                            replyMSG_2.clickName = "ShowChildren";
                            replyMSG_2.contents = ((RelationshipBuilder.m_FileAttributes)m_relationship.Get_MapOfFiles[msg.item]).Get_children;
                            SendBackMessage(replyMSG_2, msg.srcIP, msg.srcPort);


                            Message replyMSG_3 = new Message();
                            replyMSG_3.Tag = MessageTag.CATEGORY;
                            replyMSG_3.clickName = "ShowParents";
                            replyMSG_3.contents = ((RelationshipBuilder.m_FileAttributes)m_relationship.Get_MapOfFiles[msg.item]).Get_parents;
                            SendBackMessage(replyMSG_3, msg.srcIP, msg.srcPort);

                        }
                    }
                    break;
            }
        }

        // -------------- Service on Upload view --------------
        void GoUpload(Message msg)
        {
            switch (msg.clickName)
            {
                case "UploadView":
                    Console.WriteLine("\n  IN Upload service \n  ");
                    Message replyMSG = new Message();
                    replyMSG.Tag = MessageTag.UPLOAD;
                    replyMSG.clickName = "GetUpCatListBox";
                    replyMSG.contents = new ArrayList(m_relationship.Get_MapOfCategories.Keys);
                    SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);

                    Message replyMSG_1 = new Message();
                    replyMSG_1.Tag = MessageTag.UPLOAD;
                    replyMSG_1.clickName = "GetUpDepListBox";
                    replyMSG_1.contents = new ArrayList(m_relationship.Get_ListOfFiles);
                    SendBackMessage(replyMSG_1, msg.srcIP, msg.srcPort);
                    break;
            }
        }

        // -------------- Service on File & Metadata view --------------
        void GoShowFile(Message msg)
        {
            switch (msg.clickName)
            {
                case "ClickFilesListBox":
                    Console.WriteLine("\n  IN Show File And Metadata service \n  ");
                    showFileContent(msg);
                    Message replyMSG = new Message();
                    replyMSG.Tag = MessageTag.SHOWFILE;
                    replyMSG.clickName = "ShowFile";
                    replyMSG.item = m_fileConent;
                    SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);

                    showMetadataFile(msg);
                    Message replyMSG_1 = new Message();
                    replyMSG_1.Tag = MessageTag.SHOWFILE;
                    replyMSG_1.clickName = "ShowMetadata";
                    replyMSG_1.item = m_fileMetadata;
                    SendBackMessage(replyMSG_1, msg.srcIP, msg.srcPort);

                    break;
            }
        }

        // -------------- Service on EditMetadata view --------------
        void GoEditView(Message msg)
        {
            switch (msg.clickName)
            {
                case "EditView":
                    Console.WriteLine("\n  IN Upload service \n  ");
                    Message replyMSG = new Message();
                    replyMSG.Tag = MessageTag.EDITMETADATA;
                    replyMSG.clickName = "GetEditCatListBox";
                    replyMSG.contents = new ArrayList(m_relationship.Get_MapOfCategories.Keys);
                    SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);

                    Message replyMSG_1 = new Message();
                    replyMSG_1.Tag = MessageTag.EDITMETADATA;
                    replyMSG_1.clickName = "GetEditDepListBox";
                    replyMSG_1.contents = new ArrayList(m_relationship.Get_ListOfFiles);
                    SendBackMessage(replyMSG_1, msg.srcIP, msg.srcPort);
                    break;
                case "updateMetadata":
                    Console.WriteLine("\n  IN Updating Metadata service \n  ");
                    // updateMetadataFile(msg);
                    break;
            }
        }

        // -------------- Service on TextQuery view --------------
        void GoTextQueryService(Message msg)
        {
            switch (msg.clickName)
            {
                case "TextQueryRequest":
                    Console.WriteLine("\n  IN TextQuery service \n  ");
                    TextQueryService(msg);
                    break;
            }
        }

        // --------------Service on Metadata view --------------
        void GoMetadataQueryService(Message msg)
        {
            switch (msg.clickName)
            {
                case "MetadataQueryRequest":
                    Console.WriteLine("\n  IN MetadataQuery service \n  ");
                    MetadataQueryService(msg);
                    break;
            }
        }

        // --------------Service on Rebuild Relationship Map view --------------
        void GoRebuildMap(Message msg)
        {
            switch (msg.clickName)
            {
                case "updateMaprequest":
                    Console.WriteLine("\n  IN UpdateMap service \n  ");
                    RelationshipBuilder m_newrelationship = new RelationshipBuilder("../Repository");
                    m_relationship = m_newrelationship;
                    Message replyMSG = new Message();
                    replyMSG.Tag = MessageTag.UPDATE;
                    replyMSG.clickName = "UpdateFinish";
                    replyMSG.contents = new ArrayList(m_relationship.Get_MapOfCategories.Keys);
                    SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);
                    break;
            }
        }

        // -------------- Core functions for MetadataQueryService --------------
        private void MetadataQueryService(Message msg)
        {
            if (msg.item != "")
            {
                m_metadataqueryres = new ArrayList();
                string[] args = msg.item.Trim(' ').Split(' ');
                List<string> mSearch = new List<string>();
                MetadataSearch m_Metadata = new MetadataSearch();
                bool flag_r = false;
                foreach (string s in args)
                {
                    if (s.StartsWith("/M"))
                    {
                        m_Metadata.getAllMessageTags(s, ref mSearch);
                    }
                    else if (s.StartsWith("/R"))
                        flag_r = true;
                }
                // 1. Check the range of search 
                // in Initialization, we get the searching range.
                m_Metadata = new MetadataSearch(args[0], args[1], flag_r, mSearch);
                m_Metadata.searchMetaData();
                // ArrayList m_return = new ArrayList();
                foreach (var fileName in m_Metadata.Get_metadataqueryfiles)
                {
                    string addtemp = "Category: ";
                    ArrayList categoryArrayList = ((RelationshipBuilder.m_FileAttributes)m_relationship.Get_MapOfFiles[fileName]).Get_categorys;
                    foreach (var cate in categoryArrayList)
                    {
                        addtemp += " " + cate.ToString();
                    }
                    addtemp = addtemp + "  ";
                    m_metadataqueryres.Add(addtemp);
                }
                for (int i = 0; i < m_Metadata.Get_metadataqueryfiles.Count; i++)
                {
                    m_metadataqueryres[i] = m_metadataqueryres[i] + " " + m_Metadata.Get_metadataquerycontents[i].ToString() + " FileName: " + m_Metadata.Get_metadataqueryfiles[i].ToString();
                }
                // sending back query result
                Message replyMSG = new Message();
                replyMSG.Tag = MessageTag.METADATAQUERY;
                replyMSG.clickName = "MetadataQueryResult";
                replyMSG.contents = m_metadataqueryres;
                SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);
            }
        }


        // -------------- Core functions for TextQueryService --------------
        private void TextQueryService(Message msg)
        {
            if (msg.item != "")
            {
                string[] args = msg.item.Trim(' ').Split(' ');
                List<string> mSearch = new List<string>();
                bool flag_r = false;
                string operation = "/A";
                foreach (var s in args)
                {
                    if (s.StartsWith("/T"))
                        mSearch.Add(s.Substring(2));
                    else if (s.StartsWith("/R"))
                        flag_r = true;
                    else if (s.StartsWith("/A"))
                        operation = "/A";
                    else if (s.StartsWith("/O"))
                        operation = "/O";
                }
                // string pattern, string path, bool flag_recursive, List<string> searching_elements, string operation
                TextSearch m_textSearch = new TextSearch(args[0], args[1], flag_r, mSearch, operation);
                m_textSearch.searhText();
                getReturnVaule(m_textSearch.return_my_search_result());

                Console.Write("\n\n  The file contains elements: ");
                foreach (var item in mSearch)
                    Console.Write(item + "  ");
                Console.Write("\n\n  Following are the list of those file: \n  ");
                foreach (var item in m_textqueryres)
                {
                    Console.Write(item + "\n  ");
                }
                // sending back query result
                Message replyMSG = new Message();
                replyMSG.Tag = MessageTag.TEXTQUERY;
                replyMSG.clickName = "TextQueryResult";
                replyMSG.contents = new ArrayList(m_textqueryres);
                SendBackMessage(replyMSG, msg.srcIP, msg.srcPort);
            }
        }

        // -------------- Core functions for getReturnVaule --------------
        private void getReturnVaule(List<string> textRes)
        {
            m_textqueryres = new ArrayList();
            m_textqueryfiles = new ArrayList();

            if (textRes.Count == 0)
                return;
            foreach (var item in textRes)
            {
                m_textqueryfiles.Add(Path.GetFileName(item));
            }
            foreach (var fileName in m_textqueryfiles)
            {
                string addtemp = "Category: ";
                ArrayList categoryArrayList = ((RelationshipBuilder.m_FileAttributes)m_relationship.Get_MapOfFiles[fileName]).Get_categorys;
                foreach (var cate in categoryArrayList)
                {
                    addtemp += " " + cate.ToString();
                }
                addtemp = addtemp + "  " + fileName;
                m_textqueryres.Add(addtemp);
            }
        }


        // -------------- Core functions for File & Metadata View to show the content of that file  --------------
        void showFileContent(Message msg)
        {
            if (!msg.item.Equals(""))
            {
                System.IO.StreamReader myFile;
                try
                {
                    myFile = new System.IO.StreamReader("..\\Repository\\" + msg.item);
                    m_fileConent = myFile.ReadToEnd();
                    myFile.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n  Error in showing file content \n  ");
                }
            }
        }

        // -------------- Core functions for File & Metadata View to show the Metadata content of that file  --------------
        void showMetadataFile(Message msg)
        {
            if (!msg.item.Equals(""))
            {
                System.IO.StreamReader myFile;
                string m_fileName = Path.GetFileNameWithoutExtension(msg.item);
                try
                {

                    myFile = new System.IO.StreamReader("..\\Repository\\" + m_fileName + ".xml" + ".xml");
                    m_fileMetadata = myFile.ReadToEnd();
                    myFile.Close();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("\n  Error in showing file Metadata \n  ");
                }

            }

        }

        static void Main(string[] args)
        {
            Console.WriteLine("The Server is Running \n");
            Console.Title = "Remote Document Vault Server";

            Server m_server = new Server();
            m_server.Run();
            Console.WriteLine("The Server is shutting down\n");  
        }
    }
}
