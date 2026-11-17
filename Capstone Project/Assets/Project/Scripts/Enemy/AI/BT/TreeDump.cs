using System.Collections.Generic;
using System.Text;

namespace Mhieu.AI.BT
{
    /// <summary>
    /// Công cụ đọc cây hành vi để hiển thị khi debug (không đổi trạng thái của cây).
    ///
    ///  - AppendRunningPath: "Root > Engage > Approach" = chuỗi node đang chạy từ gốc xuống lá.
    ///  - AppendTree: in cả cây, mỗi dòng một node, thụt lề theo độ sâu,
    ///    "* " là đang chạy, "- " là không chạy. Có thể bọc mỗi dòng bằng thẻ màu (rich text).
    ///
    /// Tên hiển thị lấy từ Node.Name; node chưa đặt tên thì dùng tên kiểu (ví dụ "MoveToTargetAction").
    /// Điều kiện bên trong Guard không hiện trong cây (Guard giữ nó ở dạng private).
    /// </summary>
    public static class TreeDump
    {
        public static string NameOf(Node node)
        {
            if (node == null) return "(null)";
            return string.IsNullOrEmpty(node.Name) ? node.GetType().Name : node.Name;
        }

        public static void AppendRunningPath(Node root, StringBuilder sb)
        {
            bool first = true;
            Node current = root;

            while (current != null && current.IsRunning)
            {
                if (!first) sb.Append(" > ");
                sb.Append(NameOf(current));
                first = false;
                current = NextOnPath(current);
            }

            if (first) sb.Append("(none)");
        }

        public static void AppendTree(Node root, StringBuilder sb,
            string runningOpen = "", string runningClose = "",
            string idleOpen = "", string idleClose = "")
        {
            if (root == null) return;
            AppendNode(root, sb, 0, runningOpen, runningClose, idleOpen, idleClose);
        }

        private static void AppendNode(Node node, StringBuilder sb, int depth,
            string runningOpen, string runningClose, string idleOpen, string idleClose)
        {
            bool running = node.IsRunning;

            sb.Append(' ', depth * 2);
            sb.Append(running ? runningOpen : idleOpen);
            sb.Append(running ? "* " : "- ");
            sb.Append(NameOf(node));
            sb.Append(running ? runningClose : idleClose);
            sb.Append('\n');

            if (node is Composite composite)
            {
                IReadOnlyList<Node> children = composite.Children;
                for (int i = 0; i < children.Count; i++)
                    AppendNode(children[i], sb, depth + 1, runningOpen, runningClose, idleOpen, idleClose);
            }
            else if (node is Decorator decorator)
            {
                AppendNode(decorator.Child, sb, depth + 1, runningOpen, runningClose, idleOpen, idleClose);
            }
        }

        // Con kế tiếp trên đường đang chạy. Với Decorator trả về con của nó; vòng lặp ở trên tự dừng
        // nếu con đó không chạy.
        private static Node NextOnPath(Node node)
        {
            if (node is Composite composite)
            {
                IReadOnlyList<Node> children = composite.Children;
                for (int i = 0; i < children.Count; i++)
                {
                    if (children[i].IsRunning) return children[i];
                }

                return null;
            }

            if (node is Decorator decorator) return decorator.Child;

            return null;
        }
    }
}
