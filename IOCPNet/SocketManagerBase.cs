using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Linq.Expressions;
using ITnmg.IOCPNet.ProtocolInterface;
using System.Diagnostics;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// IOCP Socket 服务端,客户端通用管理基类
	/// </summary>
	public abstract class SocketManagerBase
	{
		/// <summary>
		/// 调试类型
		/// </summary>
		internal const string TraceCategory = "SocketManagerBase";

		/// <summary>
		/// SocketAsyncEventArgs 池
		/// </summary>
		protected SocketAsyncEventArgsPool saePool;

		/// <summary>
		/// userToken 缓存集合
		/// </summary>
		protected ConcurrentStack<SocketUserToken> userTokenPool;

		/// <summary>
		/// 允许的最大连接数量
		/// </summary>
		protected int maxConnCount;

		/// <summary>
		/// 启动时初始化多少个连接的资源
		/// </summary>
		protected int initConnectionResourceCount;

		/// <summary>
		/// 一次读写socket的最大缓存字节数
		/// </summary>
		protected int singleBufferMaxSize;

		/// <summary>
		/// 发送超时时间, 以毫秒为单位.
		/// </summary>
		protected int sendTimeOut;

		/// <summary>
		/// 接收超时时间, 以毫秒为单位.
		/// </summary>
		protected int receiveTimeOut;

		/// <summary>
		/// 信号量,初始设为 maxConnCount
		/// </summary>
		protected Semaphore semaphore;

		/// <summary>
		/// 已连接的集合
		/// </summary>
		protected ConcurrentDictionary<Guid, SocketUserToken> connectedEntityList;


		/// <summary>
		/// 异常事件
		/// </summary>
		public event EventHandler<Exception> ErrorEvent;

		/// <summary>
		/// Socket 连接状态改变事件
		/// </summary>
		public event EventHandler<SocketStatusChangeArgs> ConnectedStatusChangeEvent;


		/// <summary>
		/// 获取已连接的连接数
		/// </summary>
		public int TotalConnectedCount
		{
			get
			{
				return connectedEntityList.Count;
			}
		}

		/// <summary>
		/// 获取总连接数
		/// </summary>
		public int TotalCount
		{
			get
			{
				return maxConnCount;
			}
		}

		/// <summary>
		/// 获取 Socket 数据处理对象
		/// </summary>
		public ISocketProtocol BufferProcess
		{
			get;
			protected set;
		}



		/// <summary>
		/// 创建服务端实例
		/// </summary>
		public SocketManagerBase()
		{
		}



		/// <summary>
		/// 初始化管理
		/// </summary>
		/// <param name="maxConnectionCount">允许的最大连接数</param>
		/// <param name="initConnectionResourceCount">启动时初始化多少个连接的资源</param>
		/// <param name="singleBufferMaxSize">每个 socket 读写缓存最大字节数, 默认为8k</param>
		/// <param name="sendTimeOut">socket 发送超时时长, 以毫秒为单位</param>
		/// <param name="receiveTimeOut">socket 接收超时时长, 以毫秒为单位</param>
		public virtual async Task InitAsync( int maxConnectionCount, int initConnectionResourceCount, ISocketProtocol bufferProcess, int singleBufferMaxSize = 8 * 1024
			, int sendTimeOut = 10000, int receiveTimeOut = 10000 )
		{
			maxConnCount = maxConnectionCount;
			this.initConnectionResourceCount = initConnectionResourceCount;
			this.singleBufferMaxSize = singleBufferMaxSize;
			this.sendTimeOut = sendTimeOut;
			this.receiveTimeOut = receiveTimeOut;

			await Task.Run( () =>
			{
				semaphore = new Semaphore( maxConnCount, maxConnCount );
				//设置初始线程数为cpu核数*2
				connectedEntityList = new ConcurrentDictionary<Guid, SocketUserToken>( Environment.ProcessorCount * 2, maxConnCount );
				//读写分离, 每个socket连接需要2个SocketAsyncEventArgs.
				saePool = new SocketAsyncEventArgsPool( initConnectionResourceCount * 2, SendAndReceiveArgsCompleted, singleBufferMaxSize );

				userTokenPool = new ConcurrentStack<SocketUserToken>();

				for ( int i = 0; i < initConnectionResourceCount; i++ )
				{
					SocketUserToken token = new SocketUserToken( bufferProcess, singleBufferMaxSize );
					token.Id = Guid.NewGuid();
					token.ReceiveArgs = saePool.Pop();
					token.SendArgs = saePool.Pop();
					userTokenPool.Push( token );
				}
			} );
		}


		/// <summary>
		/// 分析ip或域名,返回 IPEndPoint 实例
		/// </summary>
		/// <param name="domainOrIP">要监听的域名或IP</param>
		/// <param name="port">端口</param>
		/// <param name="preferredIPv4">如果用域名初始化,可能返回多个ipv4和ipv6地址,指定是否首选ipv4地址.</param>
		/// <returns>返回 IPEndPoint 实例</returns>
		protected virtual async Task<IPEndPoint> GetIPEndPoint( string domainOrIP, int port, bool preferredIPv4 = true )
		{
			IPEndPoint result = null;

			if ( !string.IsNullOrWhiteSpace( domainOrIP ) )
			{
				string ip = domainOrIP.Trim();
				IPAddress ipAddr = null;

				if ( !IPAddress.TryParse( ip, out ipAddr ) )
				{
					var addrs = await Dns.GetHostAddressesAsync( ip );

					if ( addrs == null || addrs.Length == 0 )
					{
						throw new Exception( "域名或ip地址不正确,未能解析." );
					}

					ipAddr = addrs.FirstOrDefault( k => k.AddressFamily == (preferredIPv4 ? AddressFamily.InterNetwork : AddressFamily.InterNetworkV6) );
					ipAddr = ipAddr ?? addrs.First();
				}

				result = new IPEndPoint( ipAddr, port );
			}
			else
			{
				result = new IPEndPoint( preferredIPv4 ? IPAddress.Any : IPAddress.IPv6Any, port );
			}

			return result;
		}

		/// <summary>
		/// 执行 socket 连接成功时的处理
		/// </summary>
		/// <param name="s"></param>
		/// <returns>返回 userToken</returns>
		protected virtual void ConnCompletedSuccess( Socket s )
		{
			try
			{
				if ( GetUserToken( out SocketUserToken result ) )
				{
					result.CurrentSocket = s;
					result.ReceiveArgs.UserToken = result;
					result.SendArgs.UserToken = result;

					if ( connectedEntityList.TryAdd( result.Id, result ) )
					{
						if ( !result.CurrentSocket.ReceiveAsync( result.ReceiveArgs ) )
						{
							SendAndReceiveArgsCompleted( this, result.ReceiveArgs );
						}

						if ( !result.CurrentSocket.SendAsync( result.SendArgs ) )
						{
							SendAndReceiveArgsCompleted( this, result.SendArgs );
						}

						//SocketError.Success 状态回传null, 表示没有异常
						OnConnectedStatusChange( this, result.Id, true, null );
					}
					else
					{
						FreeUserToken( result );
					}
				}
				else
				{
					CloseSocket( s );
				}
			}
			catch ( Exception ex )
			{
				CloseSocket( s );
				OnError( this, ex );
			}
		}

		/// <summary>
		/// 执行 socket 连接异常时的处理
		/// </summary>
		protected virtual void ConnCompletedError( Socket s, SocketError error, SocketUserToken token )
		{
			try
			{
				FreeUserToken( token );
			}
			catch ( Exception ex )
			{
				OnError( this, ex );
			}
			finally
			{
				CloseSocket( s );
			}
		}

		/// <summary>
		/// Socket 发送与接收完成事件执行的方法
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void SendAndReceiveArgsCompleted( object sender, SocketAsyncEventArgs e )
		{
			var token = e.UserToken as SocketUserToken;

			if ( e.SocketError == SocketError.Success )
			{
				switch ( e.LastOperation )
				{
					case SocketAsyncOperation.Receive:
						if ( token != null )
						{
							//读取数据大于0,说明连接正常
							if ( token.ReceiveArgs.BytesTransferred > 0 )
							{
								try
								{
									token.ProcessReceive();
								}
								catch ( Exception ex )
								{
									FreeUserToken( token );
									Trace.WriteLine( "SendAndReceiveArgs_Completed:" + ex.ToString(), TraceCategory );
								}
							}
							else //否则关闭连接,释放资源
							{
								FreeUserToken( token );
							}
						}
						break;
					case SocketAsyncOperation.Send:
						if ( token != null )
						{
							try
							{
								token.ProcessSend();
							}
							catch ( Exception ex )
							{
								//否则关闭连接,释放资源
								FreeUserToken( token );
								Trace.WriteLine( "SendAndReceiveArgs_Completed:" + ex.ToString(), TraceCategory );
							}
						}
						break;
					default:
						FreeUserToken( token );
						break;
				}
			}
			else
			{
				FreeUserToken( token );
			}
		}

		/// <summary>
		/// 获取一个 userToken 资源
		/// </summary>
		/// <param name="token"></param>
		/// <returns></returns>
		protected virtual bool GetUserToken( out SocketUserToken token )
		{
			bool result = false;
			token = null;

			//等待10秒,如果有空余资源,接收连接,否则断开socket.
			if ( semaphore.WaitOne( 10000 ) && !userTokenPool.TryPop( out token ) )
			{
				token = new SocketUserToken( BufferProcess, singleBufferMaxSize );
				token.Id = Guid.NewGuid();
				token.ReceiveArgs = saePool.Pop();
				token.SendArgs = saePool.Pop();
				result = true;
			}

			return result;
		}

		/// <summary>
		/// 释放 token 资源, 将 token 放回池
		/// </summary>
		/// <param name="token">要释放的 token</param>
		protected virtual void FreeUserToken( SocketUserToken token )
		{
			if ( token != null )
			{
				connectedEntityList.TryRemove( token.Id, out SocketUserToken _ );
				CloseSocket( token.CurrentSocket );
				token.CurrentSocket = null;
				userTokenPool.Push( token );
				OnConnectedStatusChange( this, token.Id, false, null );
				semaphore.Release();
			}
		}

		/// <summary>
		/// 关闭已连接 socket 集合
		/// </summary>
		/// <returns></returns>
		protected virtual void CloseConnectList()
		{
			if ( connectedEntityList != null )
			{
				foreach ( var kv in connectedEntityList )
				{
					FreeUserToken( kv.Value );
				}

				connectedEntityList.Clear();
			}
		}

		/// <summary>
		/// 关闭 socket
		/// </summary>
		/// <param name="s"></param>
		protected virtual void CloseSocket( Socket s )
		{
			if ( s != null )
			{
				try
				{
					s.Shutdown( SocketShutdown.Both );
				}
				catch ( Exception ex )
				{
					Trace.WriteLine( "CloseSocket:" + ex.ToString(), TraceCategory );
				}

				//s.DisconnectAsync( true );
				//s.Close();
				s.Dispose();
				s = null;
			}
		}



		/// <summary>
		/// 引发 Error 事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="e"></param>
		protected virtual void OnError( object sender, Exception e )
		{
			ErrorEvent?.BeginInvoke( sender, e, null, null );
		}

		/// <summary>
		/// 引发 ConnectedStatusChangeEvent 事件
		/// </summary>
		/// <param name="sender"></param>
		/// <param name="tokenId"></param>
		/// <param name="status"></param>
		/// <param name="error"></param>
		protected virtual void OnConnectedStatusChange( object sender, Guid tokenId, bool status, SocketError? error )
		{
			if ( ConnectedStatusChangeEvent != null )
			{
				var arg = new SocketStatusChangeArgs();
				arg.UserTokenId = tokenId;
				arg.Status = status;
				arg.Error = error;
				ConnectedStatusChangeEvent.BeginInvoke( sender, arg, null, null );
			}
		}

	}
}
