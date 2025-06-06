''' 
server -> student
{
    type: "request_app",
    room_id: "1",
    teacher_username: "teacher",
    username: "student1"
}


student -> server
{
   type: "return_app",
   room_id: "1",
   teacher_username: "teacher",
   username: "student1",
   apps: [
             {
               "process_name": "devenv",
               "main_window_title": "Classroom_Monitoring_System - Microsoft Visual Studio (Administrator)"
             },
             {
               "process_name": "TextInputHost",
               "main_window_title": "Windows Input Experience"
             },
             {
               "process_name": "explorer",
               "main_window_title": ""
             }
         ]
}
'''
