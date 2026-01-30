using System;
using System.Drawing;

namespace CvCommon
{
    /// <summary>
    /// 存储一对有序的浮点数，通常是矩形的宽度和高度。
    /// </summary>
    [Serializable]
    public struct CvSize : IFormattable
    {
        internal double _width;
        internal double _height;

        /// <summary>
        /// 获取一个表示静态空 <see cref="CvSize"/> 的值。
        /// </summary>
        public static CvSize Empty
        {
            get
            {
                return new CvSize(double.NegativeInfinity, double.NegativeInfinity);
            }
        }

        /// <summary>
        /// 初始化 <see cref="CvSize"/> 结构的新实例，并为其分配初始宽度和高度。
        /// </summary>
        /// <param name="width">新 <see cref="CvSize"/> 的宽度。</param>
        /// <param name="height">新 <see cref="CvSize"/> 的高度。</param>
        public CvSize(double width, double height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("宽度和高度必须为非负数。");
            }
            _width = width;
            _height = height;
        }

        /// <summary>
        /// 获取或设置此 <see cref="CvSize"/> 的 <see cref="Width"/>。
        /// </summary>
        public double Width
        {
            get { return _width; }
            set { _width = value; }
        }

        /// <summary>
        /// 获取或设置此 <see cref="CvSize"/> 的 <see cref="Height"/>。
        /// </summary>
        public double Height
        {
            get { return _height; }
            set { _height = value; }
        }

        /// <summary>
        /// 获取一个值，该值指示此 <see cref="CvSize"/> 是否为空。
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                return _width == double.NegativeInfinity;
            }
        }

        /// <summary>
        /// 比较两个 <see cref="CvSize"/> 结构是否相等。
        /// </summary>
        /// <param name="size1">要比较的第一个 <see cref="CvSize"/>。</param>
        /// <param name="size2">要比较的第二个 <see cref="CvSize"/>。</param>
        /// <returns>如果两个 <see cref="CvSize"/> 结构具有相同的 <see cref="Width"/> 和 <see cref="Height"/>，则为 true；否则为 false。</returns>
        public static bool operator ==(CvSize size1, CvSize size2)
        {
            return size1.Width == size2.Width && size1.Height == size2.Height;
        }

        /// <summary>
        /// 比较两个 <see cref="CvSize"/> 结构是否不相等。
        /// </summary>
        /// <param name="size1">要比较的第一个 <see cref="CvSize"/>。</param>
        /// <param name="size2">要比较的第二个 <see cref="CvSize"/>。</param>
        /// <returns>如果两个 <see cref="CvSize"/> 结构的 <see cref="Width"/> 或 <see cref="Height"/> 不相等，则为 true；否则为 false。</returns>
        public static bool operator !=(CvSize size1, CvSize size2)
        {
            return !(size1 == size2);
        }

        /// <summary>
        /// 确定指定对象是否为 <see cref="CvSize"/> 以及它是否包含与此 <see cref="CvSize"/> 相同的值。
        /// </summary>
        /// <param name="o">要比较的对象。</param>
        /// <returns>如果 obj 是 <see cref="CvSize"/> 并包含与此 <see cref="CvSize"/> 相同的 <see cref="Width"/> 和 <see cref="Height"/> 值，则为 true；否则为 false。</returns>
        public override bool Equals(object o)
        {
            if (o is CvSize)
            {
                CvSize other = (CvSize)o;
                return _width == other._width && _height == other._height;
            }
            return false;
        }

        /// <summary>
        /// 返回此实例的哈希码。
        /// </summary>
        /// <returns>32 位有符号整数哈希码。</returns>
        public override int GetHashCode()
        {
            return _width.GetHashCode() ^ _height.GetHashCode();
        }

        /// <summary>
        /// 返回表示当前对象的字符串。
        /// </summary>
        /// <returns>表示当前对象的字符串。</returns>
        public override string ToString()
        {
            return ToString(null, null);
        }

        /// <summary>
        /// 使用指定的格式和区域性特定格式设置返回表示当前对象的字符串。
        /// </summary>
        /// <param name="format">要使用的格式。</param>
        /// <param name="formatProvider">用于格式化值的提供程序。</param>
        /// <returns>表示当前对象的字符串。</returns>
        public string ToString(string format, IFormatProvider formatProvider)
        {
            return string.Format(formatProvider, "{0:" + format + "},{1:" + format + "}", _width, _height);
        }
    }
}
