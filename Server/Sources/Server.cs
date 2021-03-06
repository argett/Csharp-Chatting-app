﻿using System;
using System.Threading;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net.Sockets;
using System.Net;
using Network;

namespace Server
{
    public class Server
    {
        public static Semaphore semaphore = new Semaphore(1,1);
        private int port;
        private static byte nbUser;
        private static Request req;

        public Server(int n)
        {
            port = n;
            nbUser++;
            req = null;
        }

        // ---------------------------------     CONNECTION PART     ---------------------------------     

        /** *************
         * 
         * creation of an instance of a user in a thread
         * 
         ****************/
        public void start()
        {
            TcpListener input = new TcpListener(new IPAddress(new byte[] { 127, 0, 0, nbUser }), port);
            input.Start();
            while (true)
            {
                TcpClient comm = input.AcceptTcpClient();
                Console.Write("\nUser on the home page");

                new Thread(new Receiver(comm).welcomeOnTheSite).Start();
            }
        }

        class Receiver
        {
            private static TcpClient comm;

            public Receiver(TcpClient s)
            {
                comm = s;
            }


            /** *************
            * 
            * the welcome page, the user choose to create a new account or use an existing one
            * send the result at the client at the end
            * "connection denied" occure when the user has chosen to exit the program
            * 
            ****************/
            public void welcomeOnTheSite()
            {
                Console.WriteLine(", the user is choosing between create or use an existing account");
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("checkpoint message", "Hello, do you want to create a New account or connect to an Existing one ? N/E : ", false));
                waitMessage();
                if (checkConnChoice(req.getMessage())) // connect the user at the database
                {
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("connection allowed", false));
                    homePage();
                }
                else
                {
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("connection denied", false));
                    Console.WriteLine("A user is leaving the website");
                }
            }

            /** *************
             * 
             * Check the choice made in the previous function
             * 
             ****************/
            private bool checkConnChoice(string choice)
            {
                if (choice.ToLower() == "n")
                {
                    Console.WriteLine("the user is creating a new user");
                    createUser();
                    Console.WriteLine("the user is connecting");
                    if (connecting())
                        return true;
                }
                else if (choice.ToLower() == "e")
                {
                    Console.WriteLine("the user is connecting");
                    if (connecting())
                        return true;
                }

                return false;
            }

            /** *************
             * 
             * initialize a new user
             * 
             ****************/
            private void createUser()
            {
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("creation new user", "Please enter your new ID and PASSWORD", false));
                waitMessage();

                string id = "", psw = "", turn = "id";
                foreach (char s in req.getMessage())
                {
                    if (s != ' ') //  separate the psw & id from the string
                    {
                        if (turn == "id")
                            id += s;
                        else if (turn == "psw")
                            psw += s;
                        else
                            Console.WriteLine("ERROR 404 : failure parsing id/psw");
                    }
                    else
                    {
                        turn = "psw";
                    }
                }
                semaphore.WaitOne();
                Database.addNewProfile(id, psw); // insert the new user
                Database.save();
                semaphore.Release(1);

                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("checkpoint message", "Your account has been correctly created. please log in now", false));
            }

            /** *************
             * 
             * the user enter its ID & password, return true when connetced, return false if the user wants to quit
             * 
             ****************/
            private bool connecting()
            {
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("connect user", "Please enter your ID and your PASSWORD (no space) (exit as id to quit): ", false));

                while (true) // continue until the user is conncted or wants to leave
                {
                    waitMessage();
                    string[] data = separateData(req.getMessage());

                    if (data[0] == "exit")
                    {
                        endProgram();
                        return false;
                    }

                    if (connection(data[0], data[1]))
                        return true;
                }
            }

            /** *************
             * 
             * separate the complete string into and ID and a PASSWORD
             * 
             ****************/
            private string[] separateData(string msg)
            {
                string[] data = new string[2];
                string id = "", psw = "", turn = "id";
                foreach (char s in msg)
                {
                    if (s != ' ')
                    {
                        if (turn == "id")
                            id += s;
                        else if (turn == "psw")
                            psw += s;
                        else
                            Console.WriteLine("ERROR 404 : failure parsing id/psw");
                    }
                    else
                    {
                        turn = "psw";
                    }
                }
                data[0] = id;
                data[1] = psw;
                return data;
            }

            /** *************
             * 
             * check if the password and the ID correspond to an existing one
             * 
             ****************/
            private bool connection(string id, string pswrd)
            {
                semaphore.WaitOne();
                if (Database.connectProfile(id, pswrd))
                {
                    semaphore.Release(1);
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("end connection", "You are now connected to the website", false));
                    Console.WriteLine(id + " is now connected");
                    return true;
                }
                else
                {
                    semaphore.Release(1);
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("connect user", "Bad ID and or PASSWORD, please retry (no space): ", false));
                    Console.WriteLine("identification failed");
                    return false;
                }
            }


            // ---------------------------------     HOME WEBSITE PART     --------------------------------- 

            /** **************
            * 
            * function that calls the function that has been chosen by the user
            * 
            ****************/
            private void homePage()
            {
                bool _continue = true;
                while (_continue)
                {
                    Console.WriteLine("The user is in the homepage");
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("home page choice", "What do you want to do ? \n1 : Send a private message \n2 : Connect to a topic \n3 : Create a topic \n4 : Add a new friend \n5 : exit \nEnter 1/2/3/4/5 :", false));
                    waitMessage();
                    switch (req.getNumber())
                    {
                        case 1:
                            Console.WriteLine("The user goes to the private message webpage");
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", false));
                            privateMsg();
                            break;
                        case 2:
                            Console.WriteLine("The user goes to the topics webpage");
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("goto topics", false));
                            connTopic();
                            break;
                        case 3:
                            Console.WriteLine("The user goes to the create topic webpage");
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("create topic", false));
                            createTopic();
                            break;
                        case 4:
                            Console.WriteLine("The user goes to add a friend webpage");
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("add friend", false));
                            addFriend();
                            break;
                        case 5:
                            Console.WriteLine("The user quit the programm");
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("end connection", false));
                            endProgram();
                            _continue = false;
                            break;
                    }
                }
            }

            // ---------------------------------     PRIVATE MESSAGE PART     --------------------------------- 

            /** **************
            * 
            * synchronise the requests/answers
            * 
            ****************/
            private void privateMsg()
            {
                waitMessage();
                listConversation(findProfile(req.getMessage())); // print all conversation of the profile
            }

            /** **************
            * 
            * send to the Client a string containing all the topics
            * 
            ****************/
            private void listConversation(Profile p)
            {
                List<String> nameChoosen = new List<string>();
                string allConv = "Choose the conversation you want to join (enter the number):\n";
                int i = 1;
                foreach (string convName in p.getConversations().Keys)
                {
                    allConv += i.ToString();
                    allConv += " : ";
                    allConv += convName;
                    allConv += "\n";
                    nameChoosen.Add(convName);
                    i++;
                }
                allConv += i.ToString();
                allConv += " : ";
                allConv += "Create a new conversation\n";
                i++;
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", allConv, i, false));
                waitMessage();
                if (req.getNumber() == i - 1)
                {
                    Console.WriteLine("The User creates a new private conversation");
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", "create new", false));
                    createNewConv(req.getMessage());
                }
                else
                {
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", "join existing", i - 1, false));
                    conversationcPage(req.getMessage(), nameChoosen[req.getNumber() - 1]);
                }
            }

            /** **************
            * 
            * send to the Client the whole conversation with comments in a string 
            * 
            ****************/
            private void conversationcPage(string name, string titleDiscussion)
            {
                Profile p = findProfile(name);
                Conversation conv = p.getConversationWithName(titleDiscussion);

                bool _continue = true;
                bool help = false;
                while (_continue)
                {
                    int i = 0;
                    string printConv = "\t/////////////////////////////////////////////////\n";
                    printConv += "\t\t\t  " + titleDiscussion;
                    printConv += "\n\t/////////////////////////////////////////////////\n\n\n";
                    foreach (Comment c in conv.getDiscussion())
                    {
                        printConv += "     ";
                        printConv += c.getUser();
                        printConv += "\t-\t";
                        printConv += conv.getTimingN(i);
                        printConv += " :\n\n";
                        printConv += c.getMessage();
                        printConv += "\n------------------------------------\n";
                        i++;
                    }

                    if (help)
                    {
                        printConv += "\nhere are the existing commands : \n/surname <>  - to rename your friend surname\n";
                        printConv += "(don't put the \"<>\")";
                        help = false;
                    }

                    printConv += "\n\n\t***** ENTER A MESSAGE OR ENTER 'EXIT' TO QUIT *** /help TO SEE COMMANDS *****\n";
                    
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", printConv, false));
                    waitMessage();
                    switch (getString(req.getMessage().ToUpper(),1))
                    {
                        case "EXIT":
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("home page redirection", false));
                            _continue = false;
                            break;
                        case "/HELP":
                            help = true;
                            break;
                        case "/SURNAME":
                            conv.setSurname(p, getString(req.getMessage(), 2));
                            break;
                        case "":
                            break; // do not empty messages
                        default:
                            conv.addMessage(p, req.getMessage());
                            break;
                    }                        
                }
            }

            /** **************
            * 
            * send to the Client instructions on how to create a new conversation
            * get the answer and create or not the new conversation if no friends
            * 
            ****************/
            private void createNewConv(string nameUser)
            {
                Profile p = findProfile(nameUser);
                if (p.getFriends().Count != 0)
                {
                    string form = "Please enter first the new name of this conversation \nThen choose with which of your friends you want to create a conversation : \n";
                    int nbFriend = 1;
                    foreach (Profile friend in p.getFriends())
                    {
                        form += nbFriend.ToString();
                        form += " : ";
                        form += friend.login;
                        form += "\n";
                        nbFriend++;
                    }

                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("creation private message", form, nbFriend, false));
                    waitMessage();

                    // creation of the new conversation in both profile
                    Conversation conv = new Conversation(p, p.getFriends()[req.getNumber() - 1]);
                    p.addConversation(req.getMessage(), conv);
                    p.getFriends()[req.getNumber() - 1].addConversation(req.getMessage(), conv);
                    Console.WriteLine("New conversation added bewteen " + p.login + " and " + p.getFriends()[req.getNumber() - 1].login);
                    // we go to this conversation
                    conversationcPage(nameUser, req.getMessage());
                }
                else
                {
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("private message", "You have no friends (sorry)", true));
                    Thread.Sleep(1500); // give the time to the user to see the message
                }

            }

            // ---------------------------------     CONSULT TOPICS PART     --------------------------------- 

            /** **************
            * 
            * function that asks the topic chosen by the user and call the display topic function
            * 
            ****************/
            private void connTopic()
            {
                listTopics();
                waitMessage();
                int topicN = req.getNumber() - 1;
                Profile user = findProfile(req.getMessage());

                semaphore.WaitOne();
                Console.WriteLine("The user has chosen the topic " + Database.getTopic(topicN).Title);
                Database.getTopic(topicN).addMember(user);
                semaphore.Release(1);

                topicPage(topicN);
            }


            /** **************
            * 
            * send to the Client the list of the topics existing
            * 
            ****************/
            private void listTopics()
            {
                string topics = "Choose a topic among these topics (enter the number):\n";
                int i = 1;

                semaphore.WaitOne();
                foreach (Topic t in Database.getTopics())
                {
                    topics += i.ToString();
                    topics += " : ";
                    topics += t.Title;
                    topics += "\n";
                    i++;
                }
                semaphore.Release(1);

                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("topics", topics, i, false));
            }

            /** **************
            * 
            * send to the Client the whole topic with comments in a string 
            * 
            ****************/
            private void topicPage(int i)
            {
                bool _continue = true;
                bool help = false;
                bool seeUsers = false;
                while (_continue)
                {

                    semaphore.WaitOne();
                    string printTopic = "\t/////////////////////////////////////////////////\n";
                    printTopic += "\t\t\t  " + Database.getTopic(i).Title.ToUpper() + "\n";
                    printTopic += "\t/////////////////////////////////////////////////\n\n\n";
                    foreach (Comment c in Database.getTopic(i).Discussion)
                    {
                        printTopic += "     ";
                        printTopic += c.getUser();
                        printTopic += " :\n\n";
                        printTopic += c.getMessage();
                        printTopic += "\n------------------------------------\n";
                    }
                    semaphore.Release(1);

                    if (help)
                    {
                        printTopic += "\nhere are the existing commands : \n/users  - to see the list of the users in this topic\n";
                        printTopic += "(don't put the \"<>\")";
                        help = false;
                    }
                    if (seeUsers)
                    {
                        printTopic += "\nList of the users connected to this topic :\n";
                        foreach (Profile p in Database.getTopic(i).getMembers())
                            printTopic += p.login + " - ";
                        seeUsers = false;
                    }

                    printTopic += "\n\n\t*****ADD A COMMENT OR ENTER 'EXIT' TO QUIT *** /help TO SEE COMMANDS *****\n";

                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("topic", printTopic, false));
                    waitMessage();

                    switch (getString(req.getMessage().ToUpper(), 1))
                    {
                        case "EXIT":
                            Network.Net.sendMsg(comm.GetStream(), new Network.Answer("home page redirection", false));
                            _continue = false;
                            break;
                        case "/HELP":
                            help = true;
                            break;
                        case "/USERS":
                            seeUsers = true;
                            break;
                        case "":
                            break; // do not empty messages
                        default:
                            semaphore.WaitOne();
                            Database.getTopic(i).addComment(req.getTarget(), req.getMessage());
                            semaphore.Release(1);
                            break;
                    }

                }
            }

            // ---------------------------------     CREATE TOPICS PART     --------------------------------- 

            /** **************
            * 
            * send to the Client instructions on how to create a topic
            * create the new topic if it can and call the corresponding displaying function
            * 
            ****************/
            private void createTopic()
            {
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("create topic", "To create a topic, please enter its name : \n", false));
                waitMessage(); // wait the name of the topic

                semaphore.WaitOne();
                int topicN = Database.addNewTopic(req.getMessage());
                semaphore.Release(1);

                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("checkpoint message", "you can send the name", false));
                waitMessage(); //get the name of the user creating the topic

                if (Database.getTopic(topicN).addMember(findProfile(req.getMessage())))
                {
                    Console.WriteLine("New topic +'" + req.getMessage() + "' created");
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("goto topic", false));
                    Database.save();

                    topicPage(topicN);
                }
                else
                {
                    Console.WriteLine("ERROR 101 : bad name of user, doesn't exists in the database. Can't add it to the topic");
                    Network.Net.sendMsg(comm.GetStream(), new Network.Answer("error", "An error has been encountered during the connection to the Topic", true));
                }
            }


            // ---------------------------------     ADD FRIEND PART     --------------------------------- 

            /** **************
            * 
            * send to the Client instructions on how to add a friend
            * send a string containing all existing users
            * takes the answer and update the database
            * 
            ****************/
            private void addFriend()
            {
                waitMessage(); // wait to get the name of the user
                string userName = req.getMessage();
                string form = "Enter the number of the friend that you want to add :\n";
                int id = 1;
                List<String> potentialFriendsName = new List<string>();
                List<Profile> friends = findProfile(req.getMessage()).getFriends(); //list of exisiting friends

                semaphore.WaitOne();
                foreach (Profile p in Database.getProfiles())
                {
                    bool find = false;
                    // dont print someone that is already the friend
                    foreach(Profile f in friends)
                    {
                        if (p.login == f.login)
                            find = true;
                    }


                    if (!find && p.login != userName) // a user can't be friend with himself
                    {
                        form += id.ToString();
                        form += " : ";
                        form += p.login;
                        form += "\n";
                        potentialFriendsName.Add(p.login);
                        id++;
                    }
                }
                semaphore.Release(1);

                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("add friend", form, id, false));
                waitMessage();
                // add the new friend to both profiles
                findProfile(userName).addFriend(findProfile(potentialFriendsName[req.getNumber() - 1]));
                findProfile(potentialFriendsName[req.getNumber() - 1]).addFriend(findProfile(userName));
                Console.WriteLine(userName + " and " + potentialFriendsName[req.getNumber() - 1] + " are friends now");

                semaphore.WaitOne();
                Database.save();
                semaphore.Release(1);
            }


            // ---------------------------------     USEFULL FUNCTIONS     --------------------------------- 

            private string getString(string s, int part)
            {
                // part is the part we want in the string seperated by a space (start at 1)
                string result = "";
                int actual_part = 1;

                foreach (char c in s)
                {
                    if (c != ' ')
                    {
                        if (actual_part == part)
                            result += c;
                    }
                    else
                        actual_part++;

                    if (actual_part > part)
                        break; // no need to continue further, we have the string we wanted
                }
                return result;
            }

            private void waitMessage()
            {
                 req = (Request)Network.Net.rcvMsg(comm.GetStream());
            }

            private static void endProgram()
            {
                Network.Net.sendMsg(comm.GetStream(), new Network.Answer("end connection", "You have chosen to exit the webpage", false));

                semaphore.WaitOne();
                Database.save();
                semaphore.Release(1);

                comm.Close();
            }

            private Profile findProfile(string name)
            {
                semaphore.WaitOne();
                foreach (Profile p in Database.getProfiles())
                {
                    if (p.login == name)
                    {
                        semaphore.Release(1);
                        return p;
                    }
                }
                semaphore.Release(1);
                return null;
            }
        }
        
    }
}
