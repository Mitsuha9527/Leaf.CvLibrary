using System;

namespace CvCommon
{
    /// <summary>
    /// 表示二维空间中的 x 和 y 坐标对。
    /// </summary>
    [Serializable]
    public struct CvPoint : IFormattable
    {
        internal double _x;
        internal double _y;

        /// <summary>
        /// 初始化 <see cref="CvPoint"/> 结构的新实例。
        /// </summary>
        /// <param name="x">点的 x 坐标。</param>
        /// <param name="y">点的 y 坐标。</param>
        public CvPoint(double x, double y)
        {
            _x = x;
            _y = y;
        }

        /// <summary>
        /// 获取或设置此 <see cref="CvPoint"/> 的 x 坐标。
        /// </summary>
        public double X
        {
            get { return _x; }
            set { _x = value; }
        }

        /// <summary>
        /// 获取或设置此 <see cref="CvPoint"/> 的 y 坐标。
        /// </summary>
        public double Y
        {
            get { return _y; }
            set { _y = value; }
        }

        /// <summary>
        /// 返回此实例的哈希码。
        /// </summary>
        /// <returns>32 位有符号整数哈希码。</returns>
        public override int GetHashCode()
        {
            return _x.GetHashCode() ^ _y.GetHashCode();
        }

        /// <summary>
        /// 确定指定对象是否为 <see cref="CvPoint"/> 以及它是否包含与此 <see cref="CvPoint"/> 相同的值。
        /// </summary>
        /// <param name="o">要比较的对象。</param>
        /// <returns>如果 obj 是 <see cref="CvPoint"/> 并包含与此 <see cref="CvPoint"/> 相同的 <see cref="X"/> 和 <see cref="Y"/> 值，则为 true；否则为 false。</returns>
        public override bool Equals(object o)
        {
            if (o is CvPoint)
            {
                CvPoint other = (CvPoint)o;
                return _x == other._x && _y == other._y;
            }
            return false;
        }

        /// <summary>
        /// 比较两个 <see cref="CvPoint"/> 结构是否相等。
        /// </summary>
        /// <param name="point1">要比较的第一个 <see cref="CvPoint"/>。</param>
        /// <param name="point2">要比较的第二个 <see cref="CvPoint"/>。</param>
        /// <returns>如果 point1 和 point2 的 <see cref="X"/> 和 <see cref="Y"/> 坐标相等，则为 true；否则为 false。</returns>
        public static bool operator ==(CvPoint point1, CvPoint point2)
        {
            return point1.X == point2.X && point1.Y == point2.Y;
        }

        /// <summary>
        /// 比较两个 <see cref="CvPoint"/> 结构是否不相等。
        /// </summary>
        /// <param name="point1">要比较的第一个 <see cref="CvPoint"/>。</param>
        /// <param name="point2">要比较的第二个 <see cref="CvPoint"/>。</param>
        /// <returns>如果 point1 和 point2 的 <see cref="X"/> 或 <see cref="Y"/> 坐标不相等，则为 true；如果相等，则为 false。</returns>
        public static bool operator !=(CvPoint point1, CvPoint point2)
        {
            return !(point1 == point2);
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
            return string.Format(formatProvider, "{0:" + format + "},{1:" + format + "}", _x, _y);
        }
    }
}
