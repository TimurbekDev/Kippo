using System;
using System.Collections.Generic;
using System.Text;

namespace Kippo.SessionStorage;

public interface ISessionStore
{
    Task<Session> GetAsync(long chatId);
    Task SaveAsync(long chatId, Session session);
    Task<bool> DeleteAsync(long chatId);
}
