using System;
using System.Drawing;

namespace CvCommon
{
    /// <summary>
    /// 表示矩形的位置和大小。
    /// </summary>
    [Serializable]
    public struct CvRect : IFormattable
    {
        internal double _x;
        internal double _y;
        internal double _width;
        internal double _height;

        private static readonly CvRect s_empty = CreateEmptyRect();

        /// <summary>
        /// 初始化 <see cref="CvRect"/> 结构的新实例，该实例由两个对角点定义。
        /// <paramref name="point1"/> 和 <paramref name="point2"/> 可以是矩形的任何两个对角点。
        /// </summary>
        public CvRect(CvPoint point1, CvPoint point2)
        {
            _x = Math.Min(point1.X, point2.X);
            _y = Math.Min(point1.Y, point2.Y);

            // This is equivalent to Math.Abs(point1.X - point2.X), but more efficient.
            _width = Math.Max(point1.X, point2.X) - _x;
            _height = Math.Max(point1.Y, point2.Y) - _y;
        }

        /// <summary>
        /// 初始化 <see cref="CvRect"/> 结构的新实例，该实例具有指定的位置和大小。
        /// </summary>
        /// <param name="location">一个 <see cref="CvPoint"/>，表示矩形的左上角。</param>
        /// <param name="size">一个 <see cref="CvSize"/>，表示矩形的宽度和高度。</param>
        public CvRect(CvPoint location, CvSize size)
        {
            if (size.IsEmpty)
            {
                this = s_empty;
            }
            else
            {
                _x = location.X;
                _y = location.Y;
                _width = size.Width;
                _height = size.Height;
            }
        }

        /// <summary>
        /// 初始化 <see cref="CvRect"/> 结构的新实例，该实例具有指定的位置、宽度和高度。
        /// </summary>
        /// <param name="x">矩形左上角的 x 坐标。</param>
        /// <param name="y">矩形左上角的 y 坐标。</param>
        /// <param name="width">矩形的宽度。</param>
        /// <param name="height">矩形的高度。</param>
        public CvRect(double x, double y, double width, double height)
        {
            if (width < 0 || height < 0)
            {
                throw new ArgumentException("宽度和高度必须为非负数。");
            }
            _x = x;
            _y = y;
            _width = width;
            _height = height;
        }

        /// <summary>
        /// 获取一个空的 <see cref="CvRect"/>，其 X 和 Y 属性为正无穷大，Width 和 Height 属性为负无穷大。
        /// </summary>
        public static CvRect Empty
        {
            get { return s_empty; }
        }

        /// <summary>
        /// 获取一个值，该值指示此 <see cref="CvRect"/> 是否为空矩形。
        /// </summary>
        public bool IsEmpty
        {
            get { return _width <= 0 || _height <= 0; }
        }

        /// <summary>
        /// 获取或设置矩形左上角的位置。
        /// </summary>
        public CvPoint Location
        {
            get { return new CvPoint(_x, _y); }
            set
            {
                if (IsEmpty)
                {
                    //throw new InvalidOperationException("无法修改空矩形。");
                }
                _x = value.X;
                _y = value.Y;
            }
        }

        /// <summary>
        /// 获取或设置此 <see cref="CvRect"/> 的大小。
        /// </summary>
        public CvSize Size
        {
            get { return new CvSize(_width, _height); }
            set
            {
                _width = value.Width;
                _height = value.Height;
            }
        }

        /// <summary>
        /// 获取或设置矩形左上角的 x 坐标。
        /// </summary>
        public double X
        {
            get { return _x; }
            set
            {
                _x = value;
            }
        }

        /// <summary>
        /// 获取或设置矩形左上角的 y 坐标。
        /// </summary>
        public double Y
        {
            get { return _y; }
            set
            {
                _y = value;
            }
        }

        /// <summary>
        /// 获取或设置矩形的宽度。
        /// </summary>
        public double Width
        {
            get { return _width; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("宽度必须为非负数。");
                }
                _width = value;
            }
        }

        /// <summary>
        /// 获取或设置矩形的高度。
        /// </summary>
        public double Height
        {
            get { return _height; }
            set
            {
                if (value < 0)
                {
                    throw new ArgumentException("高度必须为非负数。");
                }
                _height = value;
            }
        }

        /// <summary>
        /// 获取矩形左边缘的 x 坐标。
        /// </summary>
        public double Left
        {
            get { return _x; }
        }

        /// <summary>
        /// 获取矩形上边缘的 y 坐标。
        /// </summary>
        public double Top
        {
            get { return _y; }
        }

        /// <summary>
        /// 获取矩形右边缘的 x 坐标。
        /// </summary>
        public double Right
        {
            get { return _x + _width; }
        }

        /// <summary>
        /// 获取矩形下边缘的 y 坐标。
        /// </summary>
        public double Bottom
        {
            get { return _y + _height; }
        }

        /// <summary>
        /// 获取矩形的左上角坐标。
        /// </summary>
        public CvPoint TopLeft
        {
            get { return new CvPoint(Left, Top); }
        }

        /// <summary>
        /// 获取矩形的右上角坐标。
        /// </summary>
        public CvPoint TopRight
        {
            get { return new CvPoint(Right, Top); }
        }

        /// <summary>
        /// 获取矩形的左下角坐标。
        /// </summary>
        public CvPoint BottomLeft
        {
            get { return new CvPoint(Left, Bottom); }
        }

        /// <summary>
        /// 获取矩形的右下角坐标。
        /// </summary>
        public CvPoint BottomRight
        {
            get { return new CvPoint(Right, Bottom); }
        }

        /// <summary>
        /// 确定此矩形是否包含指定的点。
        /// </summary>
        /// <param name="point">要检查的点。</param>
        /// <returns>如果点位于矩形内，则为 true；否则为 false。</returns>
        public bool Contains(CvPoint point)
        {
            return Contains(point.X, point.Y);
        }

        /// <summary>
        /// 确定此矩形是否包含指定的坐标。
        /// </summary>
        /// <param name="x">要检查的点的 x 坐标。</param>
        /// <param name="y">要检查的点的 y 坐标。</param>
        /// <returns>如果坐标位于矩形内，则为 true；否则为 false。</returns>
        public bool Contains(double x, double y)
        {
            return !IsEmpty && x >= Left && x < Right && y >= Top && y < Bottom;
        }

        /// <summary>
        /// 确定此矩形是否完全包含另一个矩形。
        /// </summary>
        /// <param name="rect">要检查的矩形。</param>
        /// <returns>如果 rect 完全位于此矩形内，则为 true；否则为 false。</returns>
        public bool Contains(CvRect rect)
        {
            return !IsEmpty
                && !rect.IsEmpty
                && Left <= rect.Left
                && Right >= rect.Right
                && Top <= rect.Top
                && Bottom >= rect.Bottom;
        }

        /// <summary>
        /// 确定此矩形是否与另一个矩形相交。
        /// </summary>
        /// <param name="rect">要检查的矩形。</param>
        /// <returns>如果两个矩形相交，则为 true；否则为 false。</returns>
        public bool IntersectsWith(CvRect rect)
        {
            return !IsEmpty
                && !rect.IsEmpty
                && (rect.Left < Right)
                && (Left < rect.Right)
                && (rect.Top < Bottom)
                && (Top < rect.Bottom);
        }

        /// <summary>
        /// 将此矩形替换为它与指定矩形的交集。
        /// </summary>
        /// <param name="rect">要相交的矩形。</param>
        public void Intersect(CvRect rect)
        {
            if (!this.IntersectsWith(rect))
            {
                this = Empty;
            }
            else
            {
                double left = Math.Max(Left, rect.Left);
                double top = Math.Max(Top, rect.Top);
                _width = Math.Min(Right, rect.Right) - left;
                _height = Math.Min(Bottom, rect.Bottom) - top;
                _x = left;
                _y = top;
            }
        }

        /// <summary>
        /// 返回两个矩形的交集。
        /// </summary>
        /// <param name="rect1">第一个矩形。</param>
        /// <param name="rect2">第二个矩形。</param>
        /// <returns>两个矩形的交集。</returns>
        public static CvRect Intersect(CvRect rect1, CvRect rect2)
        {
            rect1.Intersect(rect2);
            return rect1;
        }

        /// <summary>
        /// 创建一个足以包含当前矩形和指定矩形的最小矩形。
        /// </summary>
        /// <param name="rect">要合并的矩形。</param>
        public void Union(CvRect rect)
        {
            if (IsEmpty)
            {
                this = rect;
            }
            else if (!rect.IsEmpty)
            {
                double left = Math.Min(Left, rect.Left);
                double top = Math.Min(Top, rect.Top);
                double right = Math.Max(Right, rect.Right);
                double bottom = Math.Max(Bottom, rect.Bottom);
                _x = left;
                _y = top;
                _width = right - left;
                _height = bottom - top;
            }
        }

        /// <summary>
        /// 返回两个矩形的并集。
        /// </summary>
        /// <param name="rect1">第一个矩形。</param>
        /// <param name="rect2">第二个矩形。</param>
        /// <returns>两个矩形的并集。</returns>
        public static CvRect Union(CvRect rect1, CvRect rect2)
        {
            rect1.Union(rect2);
            return rect1;
        }

        /// <summary>
        /// 按指定的量偏移矩形。
        /// </summary>
        /// <param name="offsetX">水平偏移量。</param>
        /// <param name="offsetY">垂直偏移量。</param>
        public void Offset(double offsetX, double offsetY)
        {
            if (IsEmpty)
            {
                throw new InvalidOperationException("无法修改空矩形。");
            }
            _x += offsetX;
            _y += offsetY;
        }

        /// <summary>
        /// 比较两个 <see cref="CvRect"/> 结构是否相等。
        /// </summary>
        /// <param name="rect1">要比较的第一个 <see cref="CvRect"/>。</param>
        /// <param name="rect2">要比较的第二个 <see cref="CvRect"/>。</param>
        /// <returns>如果两个矩形具有相同的位置和大小，则为 true；否则为 false。</returns>
        public static bool operator ==(CvRect rect1, CvRect rect2)
        {
            return rect1.X == rect2.X
                && rect1.Y == rect2.Y
                && rect1.Width == rect2.Width
                && rect1.Height == rect2.Height;
        }

        /// <summary>
        /// 比较两个 <see cref="CvRect"/> 结构是否不相等。
        /// </summary>
        /// <param name="rect1">要比较的第一个 <see cref="CvRect"/>。</param>
        /// <param name="rect2">要比较的第二个 <see cref="CvRect"/>。</param>
        /// <returns>如果两个矩形具有不同的位置或大小，则为 true；否则为 false。</returns>
        public static bool operator !=(CvRect rect1, CvRect rect2)
        {
            return !(rect1 == rect2);
        }

        /// <summary>
        /// 确定指定对象是否为 <see cref="CvRect"/> 以及它是否包含与此 <see cref="CvRect"/> 相同的值。
        /// </summary>
        /// <param name="o">要比较的对象。</param>
        /// <returns>如果 obj 是 <see cref="CvRect"/> 并包含与此 <see cref="CvRect"/> 相同的值，则为 true；否则为 false。</returns>
        public override bool Equals(object o)
        {
            if (o is CvRect)
            {
                CvRect rect = (CvRect)o;
                return this == rect;
            }
            return false;
        }

        /// <summary>
        /// 返回此实例的哈希码。
        /// </summary>
        /// <returns>32 位有符号整数哈希码。</returns>
        public override int GetHashCode()
        {
            if (IsEmpty)
            {
                return 0;
            }
            return X.GetHashCode() ^ Y.GetHashCode() ^ Width.GetHashCode() ^ Height.GetHashCode();
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
            if (IsEmpty)
            {
                return "Empty";
            }
            return string.Format(
                formatProvider,
                "{0:" + format + "},{1:" + format + "},{2:" + format + "},{3:" + format + "}",
                _x,
                _y,
                _width,
                _height
            );
        }

        private static CvRect CreateEmptyRect()
        {
            CvRect rect = new CvRect();
            rect._x = 0;
            rect._y = 0;
            rect._width = 0;
            rect._height = 0;
            return rect;
        }
    }
}
