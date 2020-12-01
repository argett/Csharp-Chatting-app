﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Server
{
    class Database
    {
        private List<Profile> allProfiles;
        private List<Topic> allTopics;

        public Database()
        {
            allProfiles = new List<Profile>();
            allTopics = new List<Topic>();
        }

        public bool connectProfile(string id, string psw)
        {
            // check if the profile exists
            if(allProfiles != null)
            {
                foreach (Profile pro in allProfiles)
                {
                    if (pro.login == id && pro.getPassword() == psw)
                        return true;
                }
            }

            return false;
        }

        public void addNewProfile(string id, string psw)
        {
            allProfiles.Add(new Profile(id, psw));
            Console.WriteLine("The profile '" + id + "' has been created correctly. Password = '" + psw + "'");
        }
        public void addTopic(string title)
        {
            allTopics.Add(new Topic(title));
        }

        public List<Topic> getTopics()
        {
            return allTopics;
        }
    }
}