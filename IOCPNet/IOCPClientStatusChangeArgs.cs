using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ITnmg.IOCPNet
{
	/// <summary>
	/// IOCPClient 连接状态变更事件参数
	/// </summary>
	public class IOCPClientStatusChangeArgs : EventArgs
	{
		/// <summary>
		/// 获取引发事件的 IOCPClient Id
		/// </summary>
		public Guid ClientId
		{
			get;
			internal set;
		}

		/// <summary>
		/// 获取 Socket 当前状态
		/// </summary>
		public bool Status
		{
			get;
			internal set;
		}

		/// <summary>
		/// 获取 Socket 异常信息
		/// </summary>
		public SocketError? Error
		{
			get;
			internal set;
		}
	}
}
