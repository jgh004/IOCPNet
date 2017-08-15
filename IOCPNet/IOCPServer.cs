﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// IOCP服务端
	/// </summary>
	public class IOCPServer : IOCPManagerBase
	{
		/// <summary>
		/// 监听用的 socket
		/// </summary>
		protected Socket listenSocket;

		/// <summary>
		/// 服务端运行状态变化事件
		/// </summary>
		public event EventHandler<bool> ServerStatusChangeEvent;



		/// <summary>
		/// 创建服务端实例
		/// </summary>
		public IOCPServer()
		{
		}



		/// <summary>
		/// 开始监听连接
		/// </summary>
		/// <param name="domainOrIP">要监听的域名或IP</param>
		/// <param name="port">端口</param>
		/// <param name="preferredIPv4">如果用域名初始化,可能返回多个ipv4和ipv6地址,指定是否首选ipv4地址.</param>
		/// <param name="backlog">监听的 socket 挂起连接队列的最大长度</param>
		/// <returns>返回实际监听的 EndPoint</returns>
		public virtual async Task<IPEndPoint> StartAsync( string domainOrIP, int port, bool preferredIPv4 = true, int backlog = 200 )
		{
			IPEndPoint result = null;

			try
			{
				if ( listenSocket == null )
				{
					result = await GetIPEndPoint( domainOrIP, port, preferredIPv4 );
					listenSocket = new Socket( result.AddressFamily, SocketType.Stream, ProtocolType.Tcp );
					listenSocket.SendTimeout = sendTimeOut;
					listenSocket.ReceiveTimeout = receiveTimeOut;
					listenSocket.SendBufferSize = singleBufferMaxSize;
					listenSocket.ReceiveBufferSize = singleBufferMaxSize;
					listenSocket.Bind( result );
					listenSocket.Listen( backlog );

					SocketAsyncEventArgs args = new SocketAsyncEventArgs();
					args.SetBuffer( new byte[singleBufferMaxSize], 0, singleBufferMaxSize );
					args.Completed += AcceptArgs_Completed;

					if ( !listenSocket.AcceptAsync( args ) )
					{
						AcceptArgs_Completed( listenSocket, args );
					}

					OnServerStateChange( this, true );
				}
				else
				{
					result = listenSocket.LocalEndPoint as IPEndPoint;
					OnError( this, new Exception( "服务端已在运行" ) );
				}
			}
			catch ( Exception ex )
			{
				Stop();
				OnError( this, ex );
			}

			return result;
		}

		/// <summary>
		/// 关闭监听
		/// </summary>
		public virtual void Stop()
		{
			if ( listenSocket != null )
			{
				try
				{
					CloseConnectList();
					CloseSocket( listenSocket );
					OnServerStateChange( this, false );
				}
				catch ( Exception ex )
				{
					OnError( this, ex );
				}
				finally
				{
					listenSocket = null;
				}
			}
		}


		/// <summary>
		/// 监听 socket 接收到新连接
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void AcceptArgs_Completed( object sender, SocketAsyncEventArgs e )
		{
			if ( e.SocketError == SocketError.Success )
			{
				ConnCompletedSuccess( e.AcceptSocket );
			}
			else
			{
				ConnCompletedError( e.AcceptSocket, e.SocketError, e.UserToken as IOCPClient );
			}

			//监听下一个请求
			e.AcceptSocket = null;
			e.UserToken = null;

			if ( e.SocketError != SocketError.OperationAborted && listenSocket != null && !listenSocket.AcceptAsync( e ) )
			{
				AcceptArgs_Completed( listenSocket, e );
			}
		}

		/// <summary>
		/// 引发 ServerStateChange 事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void OnServerStateChange( object sender, bool e )
		{
			ServerStatusChangeEvent?.BeginInvoke( sender, e, null, null );
		}
	}
}
