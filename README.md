RemoteDocumentVault_CSharp
==========================

This is a project of Course Software Modeling and Analysis(SMA) in Syracuse University.
The design of this software is in the Operation Concept Document(OCD) file.

The Demos are in the Screen Shot folder.

This Document Vault provides client and server. It allows users to make insertion and extraction of text files to a remote location and display information about their properties and the relationship map of those files that can be determine by its metadata file.  

The clients can upload files to server and server will generate the metadata file automatically and can also find the files by using some keywords in the metadata file. 

The project is implemented in C#, GUI is implemented by WPF, the communication between client and server is using Windows Communication Foundation(WCF).
And there are Sender and Receiver classes in the communication channel wrapper(WCF_Comm folder).

As for a course project, it has about 3.2k lines. And it's well designed, resuable and extendable.
 
