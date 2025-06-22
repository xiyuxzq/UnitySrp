using System.Runtime.InteropServices;
/// <summary>
/// 类型重新解释的扩展类
/// </summary>
public static class ReinterpretExtensions
{

	// 要将其转换为重新解释，需要使结构体的两个字段重叠，以便它们共享相同的数据
	[StructLayout(LayoutKind.Explicit)]
	struct IntFloat
	{
		//将两个字段偏移值设为0使它们重叠
		[FieldOffset(0)]
		public int intValue;
		[FieldOffset(0)]
		public float floatValue;
	}
	/// <summary>
	/// 将int类型数据重新解释为float类型
	/// </summary>
	/// <param name="value"></param>
	/// <returns></returns>
	public static float ReinterpretAsFloat(this int value)
	{
		IntFloat converter = default;
		converter.intValue = value;
		return converter.floatValue;
	}
}
