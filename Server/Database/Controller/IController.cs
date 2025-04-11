using System;
using System.Collections.Generic;

namespace App.Server.Database.Controller
{
    public interface IController<T> where T : class
    {
        IEnumerable<T> GetAll();
        T GetById(int id);
        bool Insert(T entity);
        bool Update(T entity);
        bool Delete(int id);
        bool Authenticate(int userID, string password);
    }
}