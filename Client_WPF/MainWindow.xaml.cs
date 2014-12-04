/////////////////////////////////////////////////////////////////////////////
//  MainWindow.xaml.cs  - GUI of client                                    // 
//  Language:     C#                                                       //
//  Platform:     ThinkPad T420 Windows 8                                  //
//  Application:  Remote Document Vault                                    //
//  Author:       Xincheng Lai                                             //
/////////////////////////////////////////////////////////////////////////////
//  Based on Jim Fawcett WCF_Peer_Comm Project                             //
// ver 2.2                                                                 //
// Jim Fawcett, CSE681 - Software Modeling & Analysis, Summer 2008         //
/////////////////////////////////////////////////////////////////////////////

/*
 * Module Operations
 * ========================
 * This part include the GUI for clients, and the tab will control some initialization
 * 
 * The server Url :  "http://localhost:8001/ICommunicator/Server";
 * 
 * Maintenance History:
 * ====================
 * ver 2.0 : 12/20/2013
 * ver 1.1 : 11/20/2013
 * ver 1.0 : 11/10/2013
 * - first release
 */


using System;
using System.Collections;
using System.Collections.Generic;
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
using WCF_Comm;
using System.Threading; 

using System.Net;
using System.Net.Sockets;
using System.IO;
using Microsoft.Win32;

namespace Client_WPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    { 
        private string client_IP="";
        private string client_Port="";
        private string clinet_url = "";
        private string server_url = "";

        // based on Jim Fawcett WCF project, WPF User Interface for WCF Communicator 
        private Receiver recvr;
        private Sender sndr;
        private Thread rcvThrd = null;
        delegate void NewMessage(Message msg);
        event NewMessage OnNewMessage;

        private Message recvrMsg = null;

        // for uploading file and generate metadata file
        string uploadFileName = "";
        string uploadDescription = "";
        ArrayList uploadDependency = null;
        ArrayList uploadCategories = null;
        ArrayList uploadKeyWords = null;

        // for downloading file
        private string ToSendPath = "..\\..\\ToSend";
        private string SavePath = "../DownloadFiles";
        private int BlockSize = 1024;
        private byte[] block;

        // for modify metadata 
        string editDescription = "";
        ArrayList editDependency = null;
        ArrayList editCategories = null;
        ArrayList editKeyWords = null;
        string modifyMetaFileName = null;

        private string categorySelected = "";
        private string curSelectedFile = "";
        private string curSelectedMetadata = "";
        
        public MainWindow()
        {
            InitializeComponent();
            InitializeClientIP(); 
        }

        //----< called by UI thread when dispatched from rcvThrd >-------
        // This will controled the transmit of views on client side
        void OnNewMessageHandler(Message msg)
        {
            switch (msg.Tag)
            {
                case MessageTag.CATEGORY:
                    CategoryView(msg);
                    break;
                case MessageTag.UPLOAD:
                    UploadView(msg);
                    break;
                case MessageTag.SHOWFILE:
                    FileMetadataView(msg);
                    break;
                case MessageTag.EDITMETADATA:
                    EditMeatadataView(msg);
                    break; 
                case MessageTag.TEXTQUERY:
                    ShowTextQueryResult(msg);
                    break;
                case MessageTag.METADATAQUERY:
                    ShowMetadataQueryResult(msg); // MetadataQueryResult
                    break;
                case MessageTag.UPDATE:
                    ShowRebuiltView(msg); // UpdateFinish
                    break;
            }
        }
         
        // ------------ InitializeClient IP info -------------------
        private void InitializeClientIP()
        {
            client_IP = GetIPAddress();
            if ( client_IP == "" )
                LocalIP.Content = "Can't Get Client IP";
            LocalIP.Content = "Local IP: "+ client_IP; 
        }

        // ------------ InitializeClient Port info -------------------
        private void InitializeClientPort()
        {
            client_Port = Port.Text;
        }

        public string GetIPAddress()
        {
            IPHostEntry m_host;
            m_host = Dns.GetHostEntry(Dns.GetHostName());
            foreach (IPAddress ip in m_host.AddressList)
            {
                if (ip.AddressFamily == AddressFamily.InterNetwork)
                {
                    return ip.ToString(); 
                }
            }
            return "";
        }

        // ----< start listener >-----------------------------------------
        private void OpenListener()
        {
            try
            {
                recvr = new Receiver();
                recvr.CreateRecvChannel(clinet_url); 

                rcvThrd = new Thread(new ThreadStart(this.ThreadProc));
                rcvThrd.IsBackground = true;
                rcvThrd.Start();
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                StringBuilder msg = new StringBuilder(ex.Message);
                msg.Append("\nport = ");
                msg.Append(client_Port.ToString());
                temp.Content = msg.ToString();
                temp.Height = 100;
                temp.Width = 500;
                temp.Show();
            }
        }

        // ----< send message to connected listener >---------------------
        private void SendMessage(Message message)
        {
            try
            { 
                sndr.PostMessage(message); 
            }
            catch (Exception ex)
            {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }
        }

        //----< receive thread processing >------------------------------
        void ThreadProc()
        {
            while (true)
            {
                // get message out of receive queue - will block if queue is empty
                recvrMsg = recvr.GetMessage();

                // call window functions on UI thread
                this.Dispatcher.BeginInvoke(
                  System.Windows.Threading.DispatcherPriority.Normal,
                  OnNewMessage,
                  recvrMsg);
            }
        }

        //-----------< Handle CATEGORY msg from server >------------------------------------
        private void CategoryView(Message msg)
        {
            switch ( msg.clickName )
            {
                
                case "CategoryList":
                    CategoryListBox.Items.Clear();
                    ArrayList categoryList = msg.contents;
                    foreach (var item in categoryList )
                        CategoryListBox.Items.Add(item);
                    break; 
                case "FilesListBoxChanged":
                    FilesListBox.Items.Clear();
                    ArrayList FilesList = msg.contents;
                    foreach (var item in FilesList)
                        FilesListBox.Items.Add(item);
                    break; 
                case "ShowChildren" :
                    ChildrenListBox.Items.Clear();
                    ArrayList ChildrenList = msg.contents;
                    foreach (var item in ChildrenList)
                        ChildrenListBox.Items.Add(item);
                    break; 
                case "ShowParents" :
                    ParentListBox.Items.Clear();
                    ArrayList ParentList = msg.contents;
                    foreach (var item in ParentList)
                        ParentListBox.Items.Add(item);
                    break; 
            }
        }

        //-----------< Handle CATEGORY msg from server >------------------------------------
        private void UploadView(Message msg)
        {
            switch (msg.clickName)
            {
                case "GetUpCatListBox":
                    UpCatListBox.Items.Clear();
                    ArrayList categoryList = msg.contents;
                    foreach (var item in categoryList)
                        UpCatListBox.Items.Add(item);
                    break;
                case "GetUpDepListBox":
                    UpDepListBox.Items.Clear();
                    ArrayList DepList = msg.contents;
                    foreach (var item in DepList)
                        UpDepListBox.Items.Add(item);
                    break; 
            }
        }

        //-----------< Handle ShowFile msg from server >------------------------------------
        private void FileMetadataView(Message msg)
        {
            switch (msg.clickName)
            {
                case "ShowFile":
                    FileContent.Text = msg.item;
                    break;
                case "ShowMetadata":
                    MetadataContent.Text = msg.item;
                    break;
            }
        }

        //-----------< Handle EditMetadata msg from server >------------------------------------
        private void EditMeatadataView(Message msg)
        {
            switch (msg.clickName)
            {
                case "GetEditCatListBox":
                    EditCatListBox.Items.Clear();
                    ArrayList categoryList = msg.contents;
                    foreach (var item in categoryList)
                        EditCatListBox.Items.Add(item);
                    break;
                case "GetEditDepListBox":
                    EditDepListBox.Items.Clear();
                    ArrayList DepList = msg.contents;
                    foreach (var item in DepList)
                        EditDepListBox.Items.Add(item);
                    break;
            }
        }

        //-----------< Handle TextQuery msg from server >------------------------------------
        private void ShowTextQueryResult(Message msg)
        {
            switch (msg.clickName)
            {
                case "TextQueryResult":
                    TextQueryResult.Items.Clear();
                    ArrayList TextQueryResultList = msg.contents;
                    foreach (var item in TextQueryResultList)
                        TextQueryResult.Items.Add(item);
                    break; 
            }
        } 

        //-----------< Handle TextQuery msg from server >------------------------------------
        private void ShowMetadataQueryResult(Message msg)
        {
            switch (msg.clickName)
            {
                case "MetadataQueryResult":
                    MetadataQueryResult.Items.Clear();
                    ArrayList MetadataQueryResultList = msg.contents;
                    foreach (var item in MetadataQueryResultList)
                        MetadataQueryResult.Items.Add(item);
                    break; 
            }
        } 

        //-----------< Handle UpdateView msg from server >------------------------------------
        private void ShowRebuiltView(Message msg)
        {
            switch (msg.clickName)
            {
                case "UpdateFinish":
                    FilesListBox.Items.Clear();
                    CategoryListBox.Items.Clear();
                    ParentListBox.Items.Clear();
                    ChildrenListBox.Items.Clear();

                    ArrayList categoryList = msg.contents;
                    foreach (var item in categoryList )
                        CategoryListBox.Items.Add(item);
                    break; 
            }
        }  
         
        //------------ Control Tabs and send message to the server ---------------
        private void TabControl_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            switch (TabControl.SelectedIndex)
            {  
                case 2:
                    initupLoad();
                    break;
                case 3:
                    initEditMetadata();
                    break; 
            }
            e.Handled = true;
        }

        //-------------- InitializeClient View --- -------------------
        private void initCategory()
        {
            CategoryListBox.Items.Clear();
            FilesListBox.Items.Clear();
            ParentListBox.Items.Clear();
            ChildrenListBox.Items.Clear();

            Message msg = new Message();
            msg.srcPort = client_Port.ToString();
            msg.srcIP = client_IP;
            msg.clickName = "CategoryView";
            msg.Tag = MessageTag.CATEGORY;
            SendMessage(msg);
        }

        //---------------- Update the Relationship Map and Re-render client View --- -------------------
        private void updateMap()
        {
            CategoryListBox.Items.Clear();
            FilesListBox.Items.Clear();
            ParentListBox.Items.Clear();
            ChildrenListBox.Items.Clear();

            Message msg = new Message();
            msg.srcPort = client_Port.ToString();
            msg.srcIP = client_IP;
            msg.clickName = "updateMaprequest";
            msg.Tag = MessageTag.UPDATE;
            SendMessage(msg);
        }

        //-------------- InitializeUpload View --- -------------------
        private void initupLoad()
        {
            if ( client_Port == "" )
                return;
            DescriptionTextBox.Text = "";
            KeywordsTextBox.Text = "";
            UpDepListBox.Items.Clear();
            UpCatListBox.Items.Clear();
            UploadFile.Text = "";

            Message msg = new Message();
            msg.srcPort = client_Port.ToString();
            msg.srcIP = client_IP;
            msg.clickName = "UploadView";
            msg.Tag = MessageTag.UPLOAD;
            SendMessage(msg);

            Upload.IsEnabled = false;
        }

        //-------------- InitializeEdit Metadata View --- -------------------
        private void initEditMetadata()
        {
            if (client_Port == "")
                return;
            EditDescriptionTextBox.Text = "";
            EditKeywordsTextBox.Text = "";
            EditDepListBox.Items.Clear();
            EditCatListBox.Items.Clear(); 

            Message msg = new Message();
            msg.srcPort = client_Port.ToString();
            msg.srcIP = client_IP;
            msg.clickName = "EditView";
            msg.Tag = MessageTag.EDITMETADATA;
            SendMessage(msg);

            UpdateButton.IsEnabled = false;
        }

        // ------< following are the Control in the view > -----------------------------

        // When the button was pushed, the client connects to the server
        private void Connect_Click(object sender, RoutedEventArgs e)
        {
            InitializeClientPort();
            clinet_url = "http://" + client_IP + ":" + client_Port + "/ICommunicator/Client";
            server_url = "http://localhost:8001/ICommunicator/Server";

            OnNewMessage += new NewMessage(OnNewMessageHandler);
            sndr = new Sender(server_url);
            OpenListener();
            initCategory();
            Connect.IsEnabled = false;
            Port.IsEnabled = false;
        }

        //-------------- UI control when CategoryListbox is selected -----------------------
        private void CategoryListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

            if ( CategoryListBox.SelectedValue != null )
            {
                string itemSelected = CategoryListBox.SelectedValue.ToString(); 
                Message msg = new Message();
                msg.srcIP = client_IP;
                msg.srcPort = client_Port;
                msg.clickName = "ClickCategoryListBox";
                msg.Tag = MessageTag.CATEGORY;
                msg.item = itemSelected;
                SendMessage(msg);
                e.Handled = true; 
                return;
            }
            else 
            {
                return;
            }
        }

        //-------------- UI control when FilesListBox is selected -----------------------
        private void FilesListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (FilesListBox.SelectedValue != null)
            {
                string itemSelected = FilesListBox.SelectedValue.ToString();
                CurrentFileTextBox.Text = itemSelected;
                CurrentFileTextBox_Copy.Text = itemSelected;
                modifyMetaFileName = itemSelected;
                Message msg = new Message();
                msg.srcIP = client_IP;
                msg.srcPort = client_Port;
                msg.clickName = "ClickFilesListBox";
                msg.Tag = MessageTag.CATEGORY;
                msg.item = itemSelected;
                SendMessage(msg);
                 
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "ClickFilesListBox";
                msg_1.Tag = MessageTag.SHOWFILE;
                msg_1.item = itemSelected;
                SendMessage(msg_1);

                e.Handled = true;
                return;
            }
            else
            {
                return;
            }
        }

        //-------------- UI control for Update button -----------------------
        private void Button_Click(object sender, RoutedEventArgs e)
        {
            //initCategory(); 
            updateMap();
        }

        //-------------- UI control for Upload Button -----------------------
        private void Upload_Click(object sender, RoutedEventArgs e)
        {
            if (uploadFileName != "")
            {
                uploadDescription = DescriptionTextBox.Text;
                string m_keywords = KeywordsTextBox.Text;
                if (m_keywords != "")
                {
                    getKeywords(m_keywords);
                }
                using (var inputStream = new FileStream(UploadFile.Text, FileMode.Open))
                {
                    FileTransferMessage msg = new FileTransferMessage();
                    msg.filename = uploadFileName;
                    msg.transferStream = inputStream;
                    msg.category = uploadCategories;
                    msg.dependency = uploadDependency;
                    msg.description = uploadDescription;
                    msg.keyword = uploadKeyWords;
                    sndr.uploadFile(msg);
                }
            }
            /*
            string test = "";
            foreach (var item in uploadKeyWords)
            {
                test += item+" ";
            }
            DescriptionTextBox.Text = test;
            */
        }

        void getKeywords(string temp_keywords)
        {
            uploadKeyWords = new ArrayList ();
            while ( temp_keywords.StartsWith("/K") )
            { 
                string temp_add = temp_keywords.Substring(2);
                if ( temp_add.IndexOf(' ') != -1  )
                { 
                    string backup = temp_add;
                    uploadKeyWords.Add( temp_add.Substring (0,temp_add.IndexOf(' ')) );
                    temp_keywords = backup.Substring(backup.IndexOf(' ')+1);
                }
                else if ( temp_add.IndexOf('\n') != -1)
                {
                    string backup = temp_add;
                    uploadKeyWords.Add( temp_add.Substring (0,temp_add.IndexOf('\n')) );
                    temp_keywords = backup.Substring(backup.IndexOf('\n')+1);
                } 
                else 
                {
                    uploadKeyWords.Add(temp_add);
                    temp_keywords = temp_keywords.Substring(2);
                } 
            } 
        }

        //-------------- UI control for browser the local files -----------------------
        private void Browser_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            string path = "";
            Nullable<bool> result = ofd.ShowDialog();
            if (result == true)
            {
                uploadFileName = System.IO.Path.GetFileName(ofd.FileName);
                path = System.IO.Path.GetDirectoryName(ofd.FileName);
                UploadFile.Text = path + "\\" + uploadFileName;
            }
        }

        //-------------- UI control when UpDepListBox is selected -----------------------
        private void UpDepListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uploadDependency = new ArrayList(UpDepListBox.SelectedItems);
            e.Handled = true;
        }

        //-------------- UI control when UpCatListBox is selected -----------------------
        private void UpCatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            uploadCategories = new ArrayList(UpCatListBox.SelectedItems);
            e.Handled = true;
            if (uploadCategories.Count > 0 )
                Upload.IsEnabled = true;
        }

        //-------------- UI control when ParentListBox is selected -----------------------
        private void ParentListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ParentListBox.SelectedValue != null)
            {
                string itemSelected = ParentListBox.SelectedValue.ToString();
                CurrentFileTextBox.Text = itemSelected;
                CurrentFileTextBox_Copy.Text = itemSelected;
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "ClickFilesListBox";
                msg_1.Tag = MessageTag.SHOWFILE;
                msg_1.item = itemSelected;
                SendMessage(msg_1);

                e.Handled = true;
                return;
            }
            else
            {
                return;
            }

        }

        //-------------- UI control when ChildrenListBox is selected -----------------------
        private void ChildrenListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (ChildrenListBox.SelectedValue != null)
            {
                string itemSelected = ChildrenListBox.SelectedValue.ToString();
                CurrentFileTextBox.Text = itemSelected;
                CurrentFileTextBox_Copy.Text = itemSelected;
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "ClickFilesListBox";
                msg_1.Tag = MessageTag.SHOWFILE;
                msg_1.item = itemSelected;
                SendMessage(msg_1);

                e.Handled = true;
                return;
            }
            else
            {
                return;
            }
        }

        //-------------- UI control for ExtractButton button  -----------------------
        private void ExtractButton_Click(object sender, RoutedEventArgs e)
        {
            string currentFileName = CurrentFileTextBox.Text;
            block = new byte[BlockSize];
            try
            {
                Stream strm = sndr.downloadFile(currentFileName);
                string rfilename = System.IO.Path.Combine(SavePath, currentFileName);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                using (var outputStream = new FileStream(rfilename, FileMode.Create)) {
                    while (true) {
                        int bytesRead = strm.Read(block, 0, BlockSize);
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
                string pre_name = System.IO.Path.GetFileNameWithoutExtension(currentFileName);
                string metadataFile = pre_name + ".xml" + ".xml";
                Stream strm_metadata = sndr.downloadFile(metadataFile);
                string rfilename_metadata = System.IO.Path.Combine(SavePath, metadataFile);
                if (!Directory.Exists(SavePath))
                    Directory.CreateDirectory(SavePath);
                using (var outputStream = new FileStream(rfilename_metadata, FileMode.Create)) {
                    while (true) {
                        int bytesRead = strm_metadata.Read(block, 0, BlockSize);
                        if (bytesRead > 0)
                            outputStream.Write(block, 0, bytesRead);
                        else
                            break;
                    }
                }
            }
            catch (Exception ex) {
                Window temp = new Window();
                temp.Content = ex.Message;
                temp.Height = 100;
                temp.Width = 500;
            }
        }

        // ------- shut the thread and then close the window -----------------
        private void Window_Unloaded(object sender, RoutedEventArgs e)
        {
            Message msg = new Message();
            msg.Tag = MessageTag.END;
            sndr.PostMessage(msg);
            sndr.Close();
            recvr.Close();
        }

        //-------------- UI control when EditDepListBox is selected  -----------------------
        private void EditDepListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editDependency = new ArrayList(EditDepListBox.SelectedItems);
            e.Handled = true;
        }

        //-------------- UI control when EditCatListBox is selected  -----------------------
        private void EditCatListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            editCategories = new ArrayList(EditCatListBox.SelectedItems);
            e.Handled = true;
            if (editCategories.Count > 0)
                UpdateButton.IsEnabled = true;
        }

        //-------------- UI control for UpdateButton   -----------------------
        private void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            if ( CurrentFileTextBox_Copy.Text != "")
            { 
                editDescription = EditDescriptionTextBox.Text;
                string m_keywords = EditKeywordsTextBox.Text;
                if (m_keywords != "")
                {
                    getKeywords(m_keywords);
                    editKeyWords = uploadKeyWords;
                }

                FileTransferMessage msg = new FileTransferMessage();
                msg.filename = CurrentFileTextBox_Copy.Text;
                Stream m_trans = new MemoryStream();
                msg.transferStream = m_trans;
                msg.category = editCategories;
                msg.dependency = editDependency;
                msg.description = editDescription;
                msg.keyword = editKeyWords;
                sndr.createMetadata(msg);
                 
            }
        }

        //-------------- UI control for TextQueryButton   -----------------------
        private void TextQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if (TextQueryArgs.Text != "")
            {
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "TextQueryRequest";
                msg_1.Tag = MessageTag.TEXTQUERY;
                msg_1.item = TextQueryArgs.Text;
                SendMessage(msg_1);

            }
            e.Handled = true;     
        }

        //-------------- UI control for MetadataQueryButton   -----------------------
        private void MetadataQueryButton_Click(object sender, RoutedEventArgs e)
        {
            if ( MetadataQueryArgs.Text != "" )
            {

                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "MetadataQueryRequest";
                msg_1.Tag = MessageTag.METADATAQUERY;
                msg_1.item = MetadataQueryArgs.Text;
                SendMessage(msg_1);

            }
            e.Handled = true; 
        }

        //--------------  UI control when TextQueryResult is selected  -----------------------
        private void TextQueryResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TextQueryResult.SelectedValue != null)
            {
                string itemSelected = TextQueryResult.SelectedValue.ToString();
                string selectedFile = findFileName(itemSelected);

                CurrentFileTextBox.Text = selectedFile;
                CurrentFileTextBox_Copy.Text = selectedFile;
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "ClickFilesListBox";
                msg_1.Tag = MessageTag.SHOWFILE;
                msg_1.item = selectedFile;
                SendMessage(msg_1);

                e.Handled = true;
                return;
            }
            else
            {
                return;
            }
        }

        //--------------  findNames after the item was selected in the QueryListbox -----------------------
        private string findFileName(string m_string)
        { 
            while ( m_string.IndexOf(" ") != -1 )
            {
                m_string = m_string.Substring( m_string.IndexOf(" ")+1 );
            }
            return m_string;
        }

        //--------------  UI control when TextQueryResult is selected  -----------------------
        private void MetadataQueryResult_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MetadataQueryResult.SelectedValue != null)
            {
                string itemSelected = MetadataQueryResult.SelectedValue.ToString();
                string selectedFile = findFileName(itemSelected);

                CurrentFileTextBox.Text = selectedFile;
                CurrentFileTextBox_Copy.Text = selectedFile;
                Message msg_1 = new Message();
                msg_1.srcIP = client_IP;
                msg_1.srcPort = client_Port;
                msg_1.clickName = "ClickFilesListBox";
                msg_1.Tag = MessageTag.SHOWFILE;
                msg_1.item = selectedFile;
                SendMessage(msg_1);

                e.Handled = true;
                return;
            }
            else
            {
                return;
            }
        }
         
    }
}
