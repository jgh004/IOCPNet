using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Text;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// IOCPClient 池
	/// </summary>
    public class IOCPClientPool
	{
		/// <summary>
		/// IOCPClient 缓存集合
		/// </summary>
		protected ConcurrentStack<IOCPClient> pool;


		/// <summary>
		/// 初始化池
		/// </summary>
		/// <param name="capacity">初始容量</param>
		public IOCPClientPool( int capacity )
		{
			pool = new ConcurrentStack<IOCPClient>();

			for ( int i = 0; i < capacity; i++ )
			{
				IOCPClient arg = CreateNew();

				if ( arg == null )
				{
					break;
				}

				pool.Push( arg );
			}
		}


		/// <summary>
		/// 入栈
		/// </summary>
		/// <param name="item">IOCPClient 实例, 不可为null</param>
		public void Push( IOCPClient item )
		{
			if ( item == null )
			{
				throw new ArgumentNullException( "item" );
			}

			pool.Push( item );
		}

		/// <summary>
		/// 出栈, 如果为空则创建新的 IOCPClient 并设置初始值返回
		/// </summary>
		/// <returns>IOCPClient 实例</returns>
		public IOCPClient Pop()
		{
			IOCPClient result;

			if ( !pool.TryPop( out result ) )
			{
				result = CreateNew();
			}

			return result;
		}

		/// <summary>
		/// 清空堆栈
		/// </summary>
		public void Clear()
		{
			pool?.Clear();
		}

		
		/// <summary>
		/// 创建新 IOCPClient
		/// </summary>
		/// <returns></returns>
		private IOCPClient CreateNew()
		{
			return new IOCPClient();
		}
	}
}
