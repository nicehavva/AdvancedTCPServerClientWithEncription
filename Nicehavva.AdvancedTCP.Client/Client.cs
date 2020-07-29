using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Runtime.Serialization.Formatters.Binary;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Nicehavva.AdvancedTCP.Client.Helpers;
using Nicehavva.AdvancedTCP.Shared.Enums;
using Nicehavva.AdvancedTCP.Shared.Messages;
using Nicehavva.AdvancedTCP.Shared.Models;
using Nicehavva.AdvancedTCP.Shared.Utility;

namespace Nicehavva.AdvancedTCP.Client
{
    public class Client
    {
        private Thread receivingThread;
        private Thread sendingThread;
        private List<ResponseCallbackObject> callBacks;
        private string privateKey;
        private Dictionary<string, string> clientPublicKey;
        private Dictionary<string, EncryptionkeyObject> clientPublicEncryptionkeys;

        List<FileUploadRequest> fileUploadRequests;
        System.Timers.Timer timerForWriteFileContent;
        #region Properties

        /// <summary>
        /// The TcpClient that is encapsulated by this client instance.
        /// </summary>
        public TcpClient TcpClient { get; set; }
        /// <summary>
        /// The ip/domain address of the remote server.
        /// </summary>
        public String Address { get; private set; }
        /// <summary>
        /// The Port that is used to connect to the remote server.
        /// </summary>
        public int Port { get; private set; }
        /// <summary>
        /// The status of the client.
        /// </summary>
        public StatusEnum Status { get; private set; }
        /// <summary>
        /// List containing all messages that is waiting to be delivered to the remote client/server
        /// </summary>
        public List<MessageBase> MessageQueue { get; private set; }
        public string PublicKey { get; private set; }



        #endregion

        #region Events

        /// <summary>
        /// Raises when a new session is requested by a remote client.
        /// </summary>
        public event Delegates.SessionRequestDelegate SessionRequest;
        /// <summary>
        /// Raises when a new text message was received by the remote session client.
        /// </summary>
        public event Action<Client, TextMessageRequest, String> TextMessageReceived;
        /// <summary>
        /// Raises when a new file upload request was received by the remote session client.
        /// </summary>
        public event Delegates.FileUploadRequestDelegate FileUploadRequest;
        /// <summary>
        /// Raises when a progress was made when a remote session client is uploading a file to this client instance.
        /// </summary>
        public event Action<Client, EventArguments.FileUploadProgressEventArguments> FileUploadProgress;
        /// <summary>
        /// Raises when the client was disconnected;
        /// </summary>
        public event Action<Client> ClientDisconnected;
        /// <summary>
        /// Raises when the remote session client was disconnected;
        /// </summary>
        public event Action<Client> SessionClientDisconnected;
        /// <summary>
        /// Raises when a new unhandled message is received.
        /// </summary>
        public event Action<Client, GenericRequest> GenericRequestReceived;
        /// <summary>
        /// Raises when the current session was ended by the remote client.
        /// </summary>
        public event Action<Client> SessionEndedByTheRemoteClient;
        #endregion

        #region Constructors

        /// <summary>
        /// Inisializes a new Client instance.
        /// </summary>
        public Client()
        {
            fileUploadRequests = new List<FileUploadRequest>();

            RSACryptoServiceProvider rsa = new RSACryptoServiceProvider();
            PublicKey = rsa.ToXmlString(false); // false to get the public key   
            privateKey = rsa.ToXmlString(true); // true to get the private key 

            clientPublicKey = new Dictionary<string, string>();
            clientPublicEncryptionkeys = new Dictionary<string, EncryptionkeyObject>();

            callBacks = new List<ResponseCallbackObject>();
            MessageQueue = new List<MessageBase>();
            Status = StatusEnum.Disconnected;
        }

        #endregion

        #region Methods

        /// <summary>
        /// Connect to a remote server.
        /// (The client will not be able to perform any operations until it is loged in and validated).
        /// </summary>
        /// <param name="address">The server ip/domain address.</param>
        /// <param name="port">The server port.</param>
        public void Connect(String address, int port)
        {
            Address = address;
            Port = port;
            TcpClient = new TcpClient();
            TcpClient.Connect(Address, Port);
            Status = StatusEnum.Connected;
            TcpClient.ReceiveBufferSize = 1024 * 1024 * 1;
            TcpClient.SendBufferSize = 1024 * 1024 * 1;
            
            receivingThread = new Thread(ReceivingMethod);
            receivingThread.IsBackground = true;
            receivingThread.Start();

            sendingThread = new Thread(SendingMethod);
            sendingThread.IsBackground = true;
            sendingThread.Start();

            timerForWriteFileContent = new System.Timers.Timer();
            timerForWriteFileContent.Elapsed += new ElapsedEventHandler(OnElapsedTime);
            timerForWriteFileContent.Interval = 100; //number in milisecinds  
            timerForWriteFileContent.Enabled = true;
        }

        private void OnElapsedTime(object sender, ElapsedEventArgs e)
        {
            try
            {
                timerForWriteFileContent.Stop();
                FileHelper.AppendAllBytes(this, fileUploadRequests, clientPublicEncryptionkeys);
                timerForWriteFileContent.Start();
            }
            catch { }
        }

        /// <summary>
        /// Disconnect from the remote server.
        /// </summary>
        public void Disconnect(bool sendDisconnectRequest=true)
        {
            MessageQueue.Clear();
            callBacks.Clear();
            clientPublicKey.Clear();
            fileUploadRequests.Clear();
            timerForWriteFileContent.Dispose();
            try
            {
                if (sendDisconnectRequest)
                {
                    SendMessage(new DisconnectRequest());
                }
            }
            catch { }
            Thread.Sleep(1000);
            Status = StatusEnum.Disconnected;
            try
            {
                TcpClient.Client.Disconnect(false);
            }
            catch { }
            TcpClient.Close();
            OnClientDisconnected();
        }

        /// <summary>
        /// Log in to the remote server.
        /// </summary>
        /// <param name="email">The email address that will be used to identify this client instance.</param>
        /// <param name="callback">Will be invoked when a Validation Response was received from remote server.</param>
        public void Login(String clientName, Action<Client, ValidationResponse> callback)
        {
            //Create a new validation request message
            ValidationRequest request = new ValidationRequest();
            request.ClientName = clientName;

            //Add a callback before we send the message
            AddCallback(callback, request);

            //Send the message (Add it to the message queue)
            SendMessage(request);
        }

        /// <summary>
        /// Request session from a remote client.
        /// </summary>
        /// <param name="email">The remote client email address (Case sensitive).</param>
        /// <param name="callback">Will be invoked when a Session Response was received from the remote client.</param>
        public void RequestSession(String clientName, Action<Client, SessionResponse> callback)
        {
            SessionRequest request = new SessionRequest();
            request.ClientName = clientName;
            request.PublicKey = PublicKey;
            AddCallback(callback, request);
            SendMessage(request);
        }

        /// <summary>
        /// Ends the current session with the remote user.
        /// </summary>
        /// <param name="callback">Will be invoked when an EndSession response was received from the server.</param>
        public void EndCurrentSession(String clientName, Action<Client, EndSessionResponse> callback)
        {
            checkReceiver(clientName);
            EndSessionRequest request = new EndSessionRequest();
            request.ClientName = clientName;
            AddCallback(callback, request);
            SendMessage(request);
        }

        /// <summary>
        /// Watch the remote client's desktop.
        /// </summary>
        /// <param name="callback">Will be invoked when a RemoteDesktop Response was received.</param>
        public void RequestDesktop(String clientName , Action<Client, RemoteDesktopResponse> callback)
        {
            checkReceiver(clientName);
            RemoteDesktopRequest request = new RemoteDesktopRequest();
            request.ReceiverClient = clientName;
            AddCallback(callback, request);
            SendMessage(request);
        }

        /// <summary>
        /// Send a text message to the remote client.
        /// </summary>
        /// <param name="message"></param>
        public void SendTextMessage(String message, String clientName)
        {
            checkReceiver(clientName);
            var encryptionkey = clientPublicEncryptionkeys[clientName];
            TextMessageRequest request = new TextMessageRequest();
            request.ReceiverClient = clientName;
            request.Message = UtilityFunction.EncryptString(message, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
            SendMessage(request);
        }

        /// <summary>
        /// Upload a file to the remote client.
        /// </summary>
        /// <param name="fileName">The full file path to the file.</param>
        /// <param name="callback">Will be invoked when a progress is made in uploading the file</param>
        public void UploadFile(String fileName, String clientName, Action<Client, FileUploadResponse> callback)
        {
            checkReceiver(clientName);
            FileUploadRequest request = new FileUploadRequest();
            request.ReceiverClient = clientName;
            request.SourceFilePath = fileName;
            request.FileName = Path.GetFileName(fileName);
            request.TotalBytes = FileHelper.GetFileLength(fileName);
            AddCallback(callback, request);
            SendMessage(request);
        }

        /// <summary>
        /// Send a message of type MessageBase to the remote client.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(MessageBase message)
        {
            MessageQueue.Add(message);
        }

        private void checkReceiver(String ReceiverClient)
        {
            if (!clientPublicKey.ContainsKey(ReceiverClient))
            {
                throw new Exception("Receiver Is Not In Your Contact");
            }
        }

        /// <summary>
        /// Send a message of type GenericRequest to the remote session client.
        /// </summary>
        /// <typeparam name="T">Type of response callback delegate.</typeparam>
        /// <param name="request">A message of type GenericRequest.</param>
        /// <param name="callBack">Callback method for the response.</param>
        public void SendGenericRequest<T>(String clientName, GenericRequest request, T callBack)
        {
            checkReceiver(clientName);
            Guid guid = Guid.NewGuid();
            request.CallbackID = guid;
            GenericRequest genericRequest = new GenericRequest(request);
            genericRequest.CallbackID = guid;
            if (callBack != null) callBacks.Add(new ResponseCallbackObject() { CallBack = callBack as Delegate, ID = guid });
            genericRequest.ReceiverClient = clientName;
            SendMessage(genericRequest);
        }

        /// <summary>
        /// Send a message of type GenericResponse to the remote session client.
        /// </summary>
        /// <param name="response">A message of type GenericResponse.</param>
        public void SendGenericResponse(GenericResponse response)
        {
            checkReceiver(response.ReceiverClient);
            GenericResponse genericResponse = new GenericResponse(response);
            genericResponse.ReceiverClient = response.ReceiverClient;
            SendMessage(genericResponse);
        }

        #endregion

        #region Threads Methods

        private void SendingMethod(object obj)
        {
            while (Status != StatusEnum.Disconnected)
            {
                if (MessageQueue.Count > 0)
                {
                    MessageBase m = MessageQueue[0];

                    BinaryFormatter f = new BinaryFormatter();
                    try
                    {
                        f.Serialize(TcpClient.GetStream(), m);
                    }
                    catch
                    {
                        Disconnect(false);
                    }

                    MessageQueue.Remove(m);
                }

                Thread.Sleep(30);
            }
        }

        private void ReceivingMethod(object obj)
        {
            while (Status != StatusEnum.Disconnected)
            {
                if (TcpClient.Available > 0)
                {
                    //try
                    //{
                    BinaryFormatter f = new BinaryFormatter();
                    f.Binder = new Shared.AllowAllAssemblyVersionsDeserializationBinder();
                    MessageBase msg = f.Deserialize(TcpClient.GetStream()) as MessageBase;
                    
                    Task.Run(() => OnMessageReceived(msg));

                    //}
                    //catch (Exception e)
                    //{
                    //    Exception ex = new Exception("Unknown message recieved. Could not deserialize the stream.", e);
                    //    OnClientError(this, ex);
                    //    Debug.WriteLine(ex.Message);
                    //}
                }
                else
                {
                    try
                    {
                        if (TcpClient.GetState() != TcpState.Established)
                        {
                            Disconnect(false);
                        }
                    }
                    catch { }
                }

                Thread.Sleep(30);
            }
        }

        #endregion

        #region Message Handlers

        protected virtual void OnMessageReceived(MessageBase msg)
        {
            Type type = msg.GetType();

            if (msg is ResponseMessageBase)
            {
                if (type == typeof(GenericResponse))
                {
                    var senderClient = msg.SenderClient;
                    msg = (msg as GenericResponse).ExtractInnerMessage();
                    msg.SenderClient = senderClient;
                }
                if (type == typeof(RemoteDesktopResponse))
                {
                    var encryptionkey = clientPublicEncryptionkeys[msg.SenderClient];
                    var decryptImage = UtilityFunction.EncryptStream((msg as RemoteDesktopResponse).FrameBytes, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
                    (msg as RemoteDesktopResponse).FrameBytes = decryptImage;
                }

                InvokeMessageCallback(msg, (msg as ResponseMessageBase).DeleteCallbackAfterInvoke);

                if (type == typeof(SessionResponse))
                {
                    SessionResponseHandler(msg as SessionResponse);
                }
                else if (type == typeof(EndSessionResponse))
                {
                    EndSessionResponseHandler(msg as EndSessionResponse);
                }
                else if (type == typeof(RemoteDesktopResponse))
                {
                    RemoteDesktopResponseHandler(msg as RemoteDesktopResponse);
                }
                else if (type == typeof(FileUploadResponse))
                {
                    FileUploadResponseHandler(msg as FileUploadResponse);
                }
            }
            else
            {
                if (type == typeof(SessionRequest))
                {
                    SessionRequestHandler(msg as SessionRequest);
                }
                else if (type == typeof(EndSessionRequest))
                {
                    EndSessionRequestHandler(msg as EndSessionRequest);
                }
                else if (type == typeof(HandShakeRequest))
                {
                    HandShakeRequestHandler(msg as HandShakeRequest);
                }
                else if (type == typeof(RemoteDesktopRequest))
                {
                    RemoteDesktopRequestHandler(msg as RemoteDesktopRequest);
                }
                else if (type == typeof(TextMessageRequest))
                {
                    TextMessageRequestHandler(msg as TextMessageRequest);
                }
                else if (type == typeof(FileUploadRequest))
                {
                    FileUploadRequestHandler(msg as FileUploadRequest);
                }
                else if (type == typeof(DisconnectRequest))
                {
                    DisconnectRequestHandler(msg as DisconnectRequest);
                    
                }
                else if (type == typeof(GenericRequest))
                {
                    OnGenericRequestReceived(msg as GenericRequest);
                }
            }
        }

        private void SessionResponseHandler(SessionResponse response)
        {
            if (response.IsConfirmed)
            {
                if (clientPublicKey.ContainsKey(response.ClientName))
                {
                    clientPublicKey[response.ClientName] = response.PublicKey;
                }
                else
                {
                    clientPublicKey.Add(response.ClientName, response.PublicKey);
                }
                HandShakeRequest handShakeRequest = new HandShakeRequest();
                EncryptionkeyObject encryptionkeyObject = new EncryptionkeyObject();
                Random rnd = new Random();

                encryptionkeyObject.FirstX= rnd.NextDouble();
                encryptionkeyObject.U= rnd.NextDouble();
                encryptionkeyObject.SelectChoas = rnd.Next(0, 2) == 0 ? ChoasEnum.First : ChoasEnum.Secend;

                if (clientPublicEncryptionkeys.ContainsKey(response.ClientName))
                {
                    clientPublicEncryptionkeys[response.ClientName] = encryptionkeyObject;
                }
                else
                {
                    clientPublicEncryptionkeys.Add(response.ClientName, encryptionkeyObject);
                }

                handShakeRequest.FirstX = UtilityFunction.EncryptByte(response.PublicKey, BitConverter.GetBytes(encryptionkeyObject.FirstX));
                handShakeRequest.U = UtilityFunction.EncryptByte(response.PublicKey, BitConverter.GetBytes(encryptionkeyObject.U));
                handShakeRequest.SelectChoas = UtilityFunction.EncryptByte(response.PublicKey, BitConverter.GetBytes((short)encryptionkeyObject.SelectChoas));
                handShakeRequest.ReceiverClient = response.ClientName;

                SendMessage(handShakeRequest);
            }
        }
        
        private void EndSessionResponseHandler(EndSessionResponse response)
        {
            if (!response.HasError)
            {
                if (clientPublicKey.ContainsKey(response.ClientName))
                {
                    clientPublicKey.Remove(response.ClientName);
                }
            }
        }
        
        private void RemoteDesktopResponseHandler(RemoteDesktopResponse response)
        {
            if (!response.Cancel)
            {
                RemoteDesktopRequest request = new RemoteDesktopRequest();
                request.CallbackID = response.CallbackID;
                request.ReceiverClient = response.SenderClient;
                SendMessage(request);
            }
            else
            {
                callBacks.RemoveAll(x => x.ID == response.CallbackID);
            }
        }

        private void FileUploadResponseHandler(FileUploadResponse response)
        {
            FileUploadRequest request = new FileUploadRequest(response);

            if (!response.HasError)
            {
                if (request.CurrentPosition == 0)
                {
                    var encryptionkey = clientPublicEncryptionkeys[request.ReceiverClient];

                    //MemoryMappedFile mmf = MemoryMappedFile.CreateFromFile(request.SourceFilePath);
                    //MemoryMappedViewStream mms = mmf.CreateViewStream();
                    using (FileStream fs = File.Open(request.SourceFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                    using (BufferedStream bs = new BufferedStream(fs))
                    using (BinaryReader fileStream = new BinaryReader(bs))
                    {
                        var bytes = new byte[request.BufferSize];
                        // Read and verify the data
                        int j = 0;
                        for (long i = 0; i < request.TotalBytes; i++, j++)
                        {
                            bytes[j] = fileStream.ReadByte();
                            if (j == request.BufferSize - 1 || i == request.TotalBytes - 1)
                            {
                                request = new FileUploadRequest(request);

                                var tmpbyte = new byte[j + 1];
                                Array.Copy(bytes, 0, tmpbyte, 0, j + 1);

                                request.BytesToWrite = UtilityFunction.EncryptByte(tmpbyte, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
                                request.DataLength = j + 1;
                                request.CurrentPosition = i + 1;
                                SendMessage(request);

                                j = -1;
                            }
                        }
                        fileStream.Close();
                        fileStream.Dispose();
                    }
                    //mms.Dispose();
                    //mmf.Dispose();
                }
            }
            else
            {
                CleanMessageQueueFromFile(response);
            }
        }

        public void CleanMessageQueueFromFile(FileUploadResponse response)
        {
            lock (MessageQueue)
            {
                MessageQueue.RemoveAll(x => (x is FileUploadResponse) && ((x as FileUploadResponse).DestinationFilePath == response.DestinationFilePath));
            }
        }
        
        private void FileUploadRequestHandler(FileUploadRequest request)
        {
            FileUploadResponse response = new FileUploadResponse(request);

            if (request.CurrentPosition == 0)
            {
                EventArguments.FileUploadRequestEventArguments args = new EventArguments.FileUploadRequestEventArguments(() =>
                {
                    //Confirm File Upload
                    response.DestinationFilePath = request.DestinationFilePath;
                    SendMessage(response);
                },
                () =>
                {
                    //Refuse File Upload
                    response.HasError = true;
                    response.Exception = new Exception("The file upload request was refused by the user!");
                    SendMessage(response);
                });

                args.Request = request;
                OnFileUploadRequest(args);
            }
            else
            {
                

                lock (fileUploadRequests)
                {
                    fileUploadRequests.Add(request);
                }

                //SendMessage(response);
                //OnUploadFileProgress(new EventArguments.FileUploadProgressEventArguments() { CurrentPosition = request.CurrentPosition, FileName = request.FileName, TotalBytes = request.TotalBytes, DestinationPath = request.DestinationFilePath });
            }
        }

        private void TextMessageRequestHandler(TextMessageRequest request)
        {
            var encryptionkey = clientPublicEncryptionkeys[request.SenderClient];
            request.Message=UtilityFunction.DecryptString(request.Message, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
            OnTextMessageReceived(request, request.Message);
        }

        private void RemoteDesktopRequestHandler(RemoteDesktopRequest request)
        {
            RemoteDesktopResponse response = new RemoteDesktopResponse(request);
            try
            {
                var image = Helpers.RemoteDesktop.CaptureScreenToMemoryStream(request.Quality);
                var encryptionkey = clientPublicEncryptionkeys[request.SenderClient];
                var encript = UtilityFunction.EncryptStream(image, encryptionkey.FirstX, encryptionkey.U, encryptionkey.SelectChoas);
                response.FrameBytes = encript;
            }
            catch (Exception e)
            {
                response.HasError = true;
                response.Exception = e;
            }
            
            SendMessage(response);
        }

        private void SessionRequestHandler(SessionRequest request)
        {
            SessionResponse response = new SessionResponse(request);

            EventArguments.SessionRequestEventArguments args = new EventArguments.SessionRequestEventArguments(() =>
            {
                //Confirm Session
                response.IsConfirmed = true;
                response.ClientName = request.ClientName;
                response.PublicKey = PublicKey;
                if (clientPublicKey.ContainsKey(request.ClientName))
                {
                    clientPublicKey[request.ClientName] = request.PublicKey;
                }
                else
                {
                    clientPublicKey.Add(request.ClientName, request.PublicKey);
                }
                SendMessage(response);
            },
            () =>
            {
                //Refuse Session
                response.IsConfirmed = false;
                response.ClientName = request.ClientName;
                SendMessage(response);
            });

            args.Request = request;
            OnSessionRequest(args);
        }

        private void EndSessionRequestHandler(EndSessionRequest request)
        {
            if (clientPublicKey.ContainsKey(request.ClientName))
            {
                clientPublicKey.Remove(request.ClientName);
            }
            OnSessionEndedByTheRemoteClient();
        }
        private void DisconnectRequestHandler(DisconnectRequest request)
        {
            if (clientPublicKey.ContainsKey(request.SenderClient))
            {
                clientPublicKey.Remove(request.SenderClient);
            }
            OnSessionClientDisconnected();
        }

        private void HandShakeRequestHandler(HandShakeRequest request)
        {
            EncryptionkeyObject encryptionkeyObject = new EncryptionkeyObject();

            encryptionkeyObject.FirstX = BitConverter.ToDouble(UtilityFunction.DecryptByte(privateKey, request.FirstX), 0);
            encryptionkeyObject.U = BitConverter.ToDouble(UtilityFunction.DecryptByte(privateKey, request.U), 0);
            encryptionkeyObject.SelectChoas = (ChoasEnum)BitConverter.ToInt16(UtilityFunction.DecryptByte(privateKey, request.SelectChoas), 0);

            if (clientPublicEncryptionkeys.ContainsKey(request.SenderClient))
            {
                clientPublicEncryptionkeys[request.SenderClient] = encryptionkeyObject;
            }
            else
            {
                clientPublicEncryptionkeys.Add(request.SenderClient, encryptionkeyObject);
            }
        }

        #endregion

        #region Callback Methods

        private void AddCallback(Delegate callBack, MessageBase msg)
        {
            if (callBack != null)
            {
                Guid callbackID = Guid.NewGuid();
                ResponseCallbackObject responseCallback = new ResponseCallbackObject()
                {
                    ID = callbackID,
                    CallBack = callBack
                };

                msg.CallbackID = callbackID;
                callBacks.Add(responseCallback);
            }
        }

        private void InvokeMessageCallback(MessageBase msg, bool deleteCallback)
        {
            var callBackObject = callBacks.SingleOrDefault(x => x.ID == msg.CallbackID);

            if (callBackObject != null)
            {
                if (deleteCallback)
                {
                    callBacks.Remove(callBackObject);
                }
                callBackObject.CallBack.DynamicInvoke(this, msg);
            }
        }

        #endregion

        #region Virtuals

        protected virtual void OnSessionRequest(EventArguments.SessionRequestEventArguments args)
        {
            if (SessionRequest != null) SessionRequest(this, args);
        }

        protected virtual void OnFileUploadRequest(EventArguments.FileUploadRequestEventArguments args)
        {
            if (FileUploadRequest != null) FileUploadRequest(this, args);
        }

        protected virtual void OnTextMessageReceived(TextMessageRequest request, String txt)
        {
            if (TextMessageReceived != null) TextMessageReceived(this,request, txt);
        }

        public virtual void OnUploadFileProgress(EventArguments.FileUploadProgressEventArguments args)
        {
            if (FileUploadProgress != null) Task.Run(()=> FileUploadProgress(this, args));
        }

        protected virtual void OnClientDisconnected()
        {
            if (ClientDisconnected != null) ClientDisconnected(this);
        }

        protected virtual void OnSessionClientDisconnected()
        {
            if (SessionClientDisconnected != null) SessionClientDisconnected(this);
        }

        protected virtual void OnGenericRequestReceived(GenericRequest request)
        {
            if (GenericRequestReceived != null) 
            {
                var genericRequest = request.ExtractInnerMessage();
                genericRequest.SenderClient = request.SenderClient;
                GenericRequestReceived(this, genericRequest);
            }
        }

        protected virtual void OnSessionEndedByTheRemoteClient()
        {
            if (SessionEndedByTheRemoteClient != null) SessionEndedByTheRemoteClient(this);
        }
        #endregion
    }
}
