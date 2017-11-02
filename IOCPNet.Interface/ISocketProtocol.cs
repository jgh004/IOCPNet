using System;
using System.Collections.Generic;
using System.Text;

namespace ITnmg.IOCPNet.Interface
{
	/// <summary>
	/// 通信协议接口
	/// </summary>
    public interface ISocketProtocol
	{
		bool Encoder( byte[] src, ref byte[] dst, object packHead );
		bool Decoder( ref byte[] waitBuffer, ref int waitLen, ref byte[] data, ref int dataLen, ref object packHead );
	}
}
