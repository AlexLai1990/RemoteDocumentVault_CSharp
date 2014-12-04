/////////////////////////////////////////////////////////////////////////////////////
// Sender.cs -  Provide Sender interface for server to send back info to clients.  //
// ver 1.0                                                                         //
// Author: Xincheng Lai Based on Jim Fawcett    WCF_CommPrototype Project          //
//                                               Project4HelpF13  Project          // 
/////////////////////////////////////////////////////////////////////////////////////
/*
 * Note:  Sender class is used for server and client side to communication with each 
 * other.
 * 
 * Interface: 
 * public Sender(string url), connect to the destine url
 * downloadFile, for service of downloading files
 * createMetadata, for servcie of creating metadata
 * uploadFile, for uploading files
 * PostMessage, for sending message
 * Close, for close connection.
 * 
 * Maintenance History:
 * ==================== 
 * ver 1.0 : 11/5/2013
 * - first release
 */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Threading;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.IO;


namespace WCF_Comm
{
    public class Sender
    {
        ICommunicator svc = null;
        Thread sndrThread = null; 
        BlockingQueue<Message> sndBlockingQ = null;

        // constructor to create connection with the Communication Services
        // bind the thread with the acturl funcftion
        public Sender(string url)
        {
            sndBlockingQ = new BlockingQueue<Message>();
            while (true)
            {
                try
                {
                    if (Connect(url))
                        break;
                }
                catch
                {
                    Console.Write("\n  Connection Failed!!\n  ");
                    Thread.Sleep(100);
                }
            }
            sndrThread = new Thread(ThreadProc);
            sndrThread.IsBackground = true;
            sndrThread.Start();
        }

        // Executing to post message from the sndBlockingQ to the Communication Services
        private void ThreadProc()
        {
            while (true)
            {
                Message msg = sndBlockingQ.deQ();
                if (msg.Tag == MessageTag.END)
                { 
                    break;
                }
                svc.PostMessage(msg);
            }
        }
          
        // making connection with communication service
        public bool Connect(string url)
        {
            for (int i = 0; i < 100; ++i)
            {
                try
                {
                    svc = CreateProxy(url);
                    return true;
                }
                catch
                {
                    Thread.Sleep(100);
                }
            }
            return false;
        }
         
        public ICommunicator CreateProxy(string url)
        {
            BasicHttpBinding binding = new BasicHttpBinding();
            EndpointAddress address = new EndpointAddress(url);
            binding.TransferMode = TransferMode.Streamed;
            binding.MaxReceivedMessageSize = 50000000;
            ChannelFactory<ICommunicator> factory = new ChannelFactory<ICommunicator>(binding, address);
            return factory.CreateChannel();
        }

        public void Close()
        {
            ((ChannelFactory<ICommunicator>)svc).Close();
        }

        public void PostMessage(Message msg)
        {
            sndBlockingQ.enQ(msg);
        }

        public void uploadFile(FileTransferMessage msg)
        {
            svc.uploadFile(msg);
        }

        public void createMetadata(FileTransferMessage msg)
        {
            svc.createMetadata(msg);
        }

        public Stream downloadFile(string filename)
        {
            return svc.downloadFile(filename);
        }
    } 
}
