using System.Runtime.InteropServices;
using UnityEngine;

namespace KWS
{
    internal class ComputeBufferObject : MonoBehaviour
    {
        [HideInInspector] [SerializeField] public int PackedSize = 0;

        [HideInInspector] [SerializeField] public uint[] ComputeBufferDataUint1;
        [HideInInspector] [SerializeField] public KWS_CoreUtils.Uint2[] ComputeBufferDataUint2;
        [HideInInspector] [SerializeField] public KWS_CoreUtils.Uint3[] ComputeBufferDataUint3;
        [HideInInspector] [SerializeField] public KWS_CoreUtils.Uint4[] ComputeBufferDataUint4;

        //public ComputeBuffer GetComputeBuffer()
        //{
        //    ComputeBuffer buffer = null;
        //    switch (PackedSize)
        //    {
        //        case 1: 
        //            buffer = new ComputeBuffer(ComputeBufferDataUint1.Length, Marshal.SizeOf(typeof(uint)));
        //            buffer.SetData(ComputeBufferDataUint1);
        //            break;
        //        case 2: 
        //            buffer = new ComputeBuffer(ComputeBufferDataUint2.Length, Marshal.SizeOf(typeof(KWS_CoreUtils.Uint2)));
        //            buffer.SetData(ComputeBufferDataUint2);
        //            break;
        //        case 3:
        //            buffer = new ComputeBuffer(ComputeBufferDataUint3.Length, Marshal.SizeOf(typeof(KWS_CoreUtils.Uint3)));
        //            buffer.SetData(ComputeBufferDataUint3);
        //            break;
        //        case 4:
        //            buffer = new ComputeBuffer(ComputeBufferDataUint4.Length, Marshal.SizeOf(typeof(KWS_CoreUtils.Uint4)));
        //            buffer.SetData(ComputeBufferDataUint4);
        //            break;
        //    }

        //    return buffer;
        //}

          public ComputeBuffer GetComputeBuffer()
          {
             switch( PackedSize )
             {
                case 1:
                   return CreateAndFillBuffer( ComputeBufferDataUint1, Marshal.SizeOf( typeof( uint ) ) );
          
                case 2:
                   return CreateAndFillBuffer( ComputeBufferDataUint2, Marshal.SizeOf( typeof( KWS_CoreUtils.Uint2 ) ) );
          
                case 3:
                   return CreateAndFillBuffer( ComputeBufferDataUint3, Marshal.SizeOf( typeof( KWS_CoreUtils.Uint3 ) ) );
          
                case 4:
                   return CreateAndFillBuffer( ComputeBufferDataUint4, Marshal.SizeOf( typeof( KWS_CoreUtils.Uint4 ) ) );
          
                default:
                   return null;
             }
          }

          // Generic helper that can chunk uploads on Xbox
          private ComputeBuffer CreateAndFillBuffer<T>( T[] data, int stride ) where T : struct
          {
             if( data == null || data.Length == 0 )
                return null;
          
             var buffer = new ComputeBuffer( data.Length, stride );
          
#if UNITY_GAMECORE || UNITY_XBOXONE || UNITY_XBOX_SERIES
             // Upload in chunks to avoid "upload buffer too small" on D3D12.
             const int chunkBytes = 32 * 1024 * 1024; // 32 MB per upload (well under the ~64 MB problematic size)
             int maxElementsPerChunk = Mathf.Max( 1, chunkBytes / stride );
            
             int offset = 0;
             while( offset < data.Length )
             {
                int count = Mathf.Min( maxElementsPerChunk, data.Length - offset );
                buffer.SetData( data, offset, offset, count );
                offset += count;
             }
#else    
             // Default path: single upload
             buffer.SetData(data);
#endif   
             return buffer;
          }
    }

}