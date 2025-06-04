### Used: 
- Python3 loadbalancer/loadbalancer.py ( create load balancing server with port 8000 default)
- Python3 servers/main.py -p 9001 -ip 127.0.0.1 ( create backend server with specific port and ip, we can change it but it has to match with loadbalancer)
- Open Classroom_Monitoring_System.sln, then select student/teacher to try
- For credential of teacher and student, take a look at servers/databases/in_memory_db.py
### Comment:
- We just working with servers directory, in there we will have:
+ database: include in_memory_db.py (manage connection and room database)
+ models: Used for getting attributes of the message from Teacher
+ protocols: Core of handling login
+ services: Session_manager
+ utils: Parser.py (mapping message type to a specific models so that we can base on it to handle type of message later)
- About client using C# (Teacher and Student):
+ Main logic is in NetworkManager.cs. 



