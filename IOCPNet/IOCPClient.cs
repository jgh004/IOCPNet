using System;
using System.Collections.Generic;
using System.Net.Sockets;
using System.Text;
using ITnmg.IOCPNet.ProtocolInterface;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// IOCP客户端
	/// </summary>
	public class IOCPClient
	{
		/// <summary>
		/// 获取或设置唯一Id
		/// </summary>
		public Guid Id
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置当前 socket 连接
		/// </summary>
		public Socket CurrentSocket
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置发送数据用的SocketAsyncEventArgs
		/// </summary>
		public SocketAsyncEventArgs ReceiveArgs
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置接收数据用的SocketAsyncEventArgs
		/// </summary>
		public SocketAsyncEventArgs SendArgs
		{
			get; set;
		}

		/// <summary>
		/// 获取或设置缓存数据处理对象(应用层协议)
		/// </summary>
		public ISocketProtocol SocketProtocol
		{
			get; set;
		}



		/// <summary>
		/// 初始化 IOCPClient 实例
		/// </summary>
		public IOCPClient()
		{
		}



		/// <summary>
		/// 处理接收到的数据
		/// </summary>
		public void ProcessReceive()
		{
			//BufferProcess.Decoder( ReceiveArgs.Buffer, ReceiveArgs.BytesTransferred );
		}

		/// <summary>
		/// 处理要发送的数据
		/// </summary>
		public void ProcessSend()
		{
		}
	}
}
