//////////////////////////////////////////////////////////////////////
// ICommunicator.cs -  Communicator Service Contract                //
// ver 2.0                                                          //
// Author: Xincheng Lai Based on Jim Fawcett  FileStreaming Project //
//////////////////////////////////////////////////////////////////////
/*
 * Creating Communication contract for server and clients.
 * 
 * public class FileTransferMessage is used for uploading and saving
 * files.
 * 
 * ENUM MessageTag is message tag for identify messages when communication
 * 
 * 
 * 
 * Maintenance History:
 * ==================== 
 * ver 1.0 : 11/10/2013
 * - first release
 */


using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.ServiceModel;
using System.ServiceModel.Web;
using System.Text;
using System.Collections;
using System.IO;

namespace WCF_Comm
{
    // NOTE: You can use the "Rename" command on the "Refactor" menu to change the interface name "IService1" in both code and config file together.
    [ServiceContract]
    public interface ICommunicator
    {
        // this is used to insert file to the remote server
        [OperationContract(IsOneWay = true)]
        void uploadFile(FileTransferMessage request);
        [OperationContract(IsOneWay = true)]
        void createMetadata(FileTransferMessage request);

        [OperationContract]
        Stream downloadFile(string fileName);

        [OperationContract(IsOneWay = true)]
        void PostMessage(Message msg);
        Message GetMessage();
         
    }
     
    [MessageContract]
    public class FileTransferMessage
    {
        [MessageHeader(MustUnderstand = true)]
        public string filename;

        [MessageBodyMember(Order = 1)]
        public Stream transferStream;

        [MessageHeader(MustUnderstand = true)]
        public string description;

        [MessageHeader(MustUnderstand = true)]
        public ArrayList dependency;

        [MessageHeader(MustUnderstand = true)]
        public ArrayList keyword;
        
        [MessageHeader(MustUnderstand = true)]
        public ArrayList category;
        
    }

    public enum MessageTag
    {
        [EnumMember]
        CATEGORY,
        [EnumMember]
        SHOWFILE,
        [EnumMember]
        TEXTQUERY,
        [EnumMember]
        METADATAQUERY,
        [EnumMember]
        UPLOAD,
        [EnumMember]
        EDITMETADATA,
        [EnumMember]
        UPDATE,
        [EnumMember]
        END
    }

    // Use a data contract as illustrated in the sample below to add composite types to service operations.
    [DataContract]
    public class Message
    {
        [DataMember]
        string sourceAddress = "";
        [DataMember]
        string sourcePort = "";
        [DataMember]
        MessageTag m_tag = MessageTag.CATEGORY;
        [DataMember]
        string m_body = "";
        [DataMember]
        string m_click = ""; 
        // m_contents is used for adding a list of stuffs
        [DataMember]
        ArrayList m_contents = new ArrayList();

        [DataMember]
        public MessageTag Tag
        {
            get { return m_tag; }
            set { m_tag = value; }
        }

        [DataMember]
        public string srcIP
        {
            get { return sourceAddress; }
            set { sourceAddress = value; }
        }
        [DataMember]
        public string srcPort
        {
            get { return sourcePort; }
            set { sourcePort = value; }
        }

        [DataMember]
        public string item
        {
            get { return m_body; }
            set { m_body = value; }
        }


        [DataMember]
        public ArrayList contents
        {
            get { return m_contents; }
            set { m_contents = value; }
        }

        [DataMember]
        public string clickName
        {
            get { return m_click; }
            set { m_click = value; }
        }

    }
}
