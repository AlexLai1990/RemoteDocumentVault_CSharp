/////////////////////////////////////////////////////////////////////
// Communication.svc.cs -  WCF Communicator Service                //
// ver 1.0                                                         //
// Author: Xincheng Lai Based on Jim Fawcett                       // 
//                                  CommunicationPrototype Project //
//                                  FileStreaming Project          //
/////////////////////////////////////////////////////////////////////
/*
 * Note: 
 * The Communication will include the receiver class and sender class
 * The server will provide services, and the client is able to call the
 * interface to get the services on the server side.
 * 
 * Some services are based on the projects Jim Fawcett provided, and here 
 * i wrap those services into the recevier class and sender class. So that
 * the server will be easily to reuse this interface.
 * 
 * Maintenance History:
 * ==================== 
 * ver 1.0 : 11/5/2013
 * - first release
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web; 
using System.ServiceModel.Channels;
using System.Text;
using System.IO;
using Project2;

namespace WCF_Comm
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the class name "Service1" in code, svc and config file together.
    // NOTE: In order to launch WCF Test Client for testing this service, please select Service1.svc or Service1.svc.cs at the Solution Explorer and start debugging.
    [ServiceBehavior(IncludeExceptionDetailInFaults = true)]
    public class Receiver : ICommunicator
    {
        // for inserting and extracting files into the Repository
        string filename;
        string savePath = "..\\Repository";
        string ToSendPath = "..\\Repository";
        int BlockSize = 1024;
        byte[] block;

        static BlockingQueue<Message> BlockingQ = null;
        ServiceHost host = null;

        public Receiver()
        {
            // Only one service, the first, should create the queue
            block = new byte[BlockSize];
            if (BlockingQ == null)
                BlockingQ = new BlockingQueue<Message>();
        }

        // Server will get messages from the clients by using this interface
        public void PostMessage(Message msg)
        {
            BlockingQ.enQ(msg);
        }

        // Since this is not a service operation only server can call 
        // The sever will deal with multiple clients, so it needs this BlockingQueue to be blocked
        // And the clients should use thread to get message from the server. 
        public Message GetMessage()
        {
            return BlockingQ.deQ();
        }
          
        //  Create ServiceHost for Communication service
        public void CreateRecvChannel(string url)
        {
            // Can't configure SecurityMode other than none with streaming.
            // This is the default for BasicHttpBinding.
            //   BasicHttpSecurityMode securityMode = BasicHttpSecurityMode.None;
            //   BasicHttpBinding binding = new BasicHttpBinding(securityMode);

            BasicHttpBinding binding = new BasicHttpBinding();
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            Uri baseAddress = new Uri(url);
            Type serviceType = typeof(Receiver);
            host = new ServiceHost(serviceType, baseAddress);
            host.AddServiceEndpoint(typeof(ICommunicator), binding, baseAddress);
            host.Open();
            return;
        }
        
        public void uploadFile(FileTransferMessage msg)
        {
            filename = msg.filename;
            string rfilename = Path.Combine(savePath, filename);
            if (!Directory.Exists(savePath))
                Directory.CreateDirectory(savePath);
            using (var outputStream = new FileStream(rfilename, FileMode.Create))
            {
                while (true)
                {
                    int bytesRead = msg.transferStream.Read(block, 0, BlockSize); 
                    if (bytesRead > 0)
                        outputStream.Write(block, 0, bytesRead);
                    else
                        break;
                }
            }
            createMetadata(msg);
            Console.Write("\n  The RemoteServer received file: \"{0}\" \n ", filename);
        }

        public Stream downloadFile(string filename)
        {
            string sfilename = Path.Combine(ToSendPath, filename);
            FileStream outStream = null;
            if (File.Exists(sfilename))
            {
                outStream = new FileStream(sfilename, FileMode.Open);
            }
            else
                throw new Exception("open failed for \"" + filename + "\"");
            Console.Write("\n  File: \"{0}\" has been successfully sent", filename);
            return outStream;
        }

        // Requirement7 Each file inserted in the repository should have a metadata file
        // call each time when submitting a file
        public void createMetadata(FileTransferMessage msg)
        {
            List<string> mOperations = new List<string>(); 
            filename = msg.filename;
            string rfilename = Path.Combine(savePath, filename);
             
            string m_description = "/T" + msg.description;
            mOperations.Add(m_description);
            if ( msg.keyword != null )
                foreach (var item in msg.keyword)
                    mOperations.Add("/K" + item);
            if (msg.dependency != null)
                foreach (var item in msg.dependency)
                    mOperations.Add("/D" + item);
            if (msg.category != null)
                foreach (var item in msg.category)
                    mOperations.Add("/C" + item);
             
            MetadataTool m_test = new MetadataTool(rfilename, mOperations);
            m_test.generateMetadata();

        }

        public void Close()
        {
            host.Close();
        }

    }


}
