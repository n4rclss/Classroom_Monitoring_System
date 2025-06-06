
using System;
using System.Collections;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Teacher
{
    internal class ListViewItemComparer : IComparer
    {
        private int columnIndex;
        public int ColumnIndex
        {
            get { return columnIndex; }
            set { columnIndex = value; }
        }

        private SortOrder sortDirection;
        public SortOrder SortDirection
        {
            get { return sortDirection; }
            set { sortDirection = value; }
        }
        public ListViewItemComparer()
        {
            this.sortDirection = SortOrder.None;
        }
        public int Compare(object x, object y)
        {
            ListViewItem listViewItemX = x as ListViewItem;
            ListViewItem listViewItemY = y as ListViewItem;

            if (listViewItemX == null || listViewItemY == null)
            {
                return 0; 
            }

            if (columnIndex >= listViewItemX.SubItems.Count || columnIndex >= listViewItemY.SubItems.Count)
            {
                return 0; 
            }

            string textX = listViewItemX.SubItems[columnIndex].Text;
            string textY = listViewItemY.SubItems[columnIndex].Text;
            int result;

            result = string.Compare(textX, textY, StringComparison.OrdinalIgnoreCase);

            if (SortDirection == SortOrder.Descending)
            {
                result = -result;
            }
            else if (SortDirection == SortOrder.None)
            {
                result = 0; 
            }


            return result;
        }
    }
}
