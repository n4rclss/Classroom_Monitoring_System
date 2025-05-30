//using System.Threading.Tasks;
//
//namespace Teacher.ClassroomManager
//{
//    public class ClassroomManager
//    {
//        private Teacher.NetworkManager.NetworkManager _netManager; // Fully qualify the type to resolve ambiguity  
//
//        public async Task JoinClassroom(string roomId)
//        {
//            await _netManager.SendAsync(new
//            {
//                type = "join",
//                room_id = roomId
//            });
//        }
//
//        public async Task SendChatMessage(string content)
//        {
//            await _netManager.SendAsync(new
//            {
//                type = "chat",
//                content
//            });
//        }
//    }
//}
