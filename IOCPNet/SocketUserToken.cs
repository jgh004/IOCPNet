using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;
using ITnmg.IOCPNet.ProtocolInterface;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// 异步 socket 关联的用户程序对象
	/// </summary>
	public class SocketUserToken
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
		/// 获取缓存数据处理对象(应用层协议)
		/// </summary>
		public ISocketProtocol SocketProtocol
		{
			get;
			private set;
		}



		/// <summary>
		/// 初始化 SocketUserToken 实例
		/// </summary>
		/// <param name="socketProtocol"></param>
		/// <param name="singleBufferMaxSize"></param>
		public SocketUserToken( ISocketProtocol socketProtocol, int singleBufferMaxSize )
		{
			SocketProtocol = socketProtocol ?? throw new ArgumentNullException( "socketProtocol" );
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
