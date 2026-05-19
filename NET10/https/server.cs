//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                     !!
//!!   https.net сервер на C#.     Автор: A.Б.Корниенко  !!
//!!   Серверный движок            версия от 19.05.2025  !!
//!!                                                     !!
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

using System;
using System.Net;
using System.Threading;
using System.Net.Sockets;
using System.Threading.Tasks;
using System.Collections.Concurrent;

namespace https1 {

  class Server {
    private const string mess="\tThe number of running tasks has exceeded allowed value of ";
    Socket listenSocket, listenSocket1;
    int stAll;
    Task[] t;         // Запуск сессий

    public bool Start(IPEndPoint ep, IPEndPoint ep1) {
      stAll = F.st + F.st2;
      t = new Task[stAll];

      listenSocket1 = CreateListenSocket(ep1, F.port1);
      listenSocket = CreateListenSocket(ep, F.port);
      if(!(listenSocket1 != null)) F.port1 = F.i0;
      if(!(listenSocket != null)) F.port = F.i0;

      //Console.WriteLine("Press any key to terminate the server process....");
      //Console.ReadKey();

      return listenSocket != null || listenSocket1 != null;
    }

    private Socket CreateListenSocket(IPEndPoint ep, int port) {
      Socket s = null;
      if(port>F.i0) {
        // create the socket which listens for incoming connections
        s = new Socket(ep.AddressFamily, SocketType.Stream, ProtocolType.Tcp) {
                NoDelay = true };            // Мгновенная отправка
        try { s.Bind(ep); } catch (Exception) { s.Close(); s = null; }

        // start the server with a listen backlog of F.qu connections
        s?.Listen(F.qu);
      }
      return s;
    }

    // Остановить сервер
    public void Stop() {

       // Закрыть все сессии
       for (int i=0; i<F.session.Length; i++) {
         if (t[i] != null) F.session[i].Stop();
       }

       // Закрыть прослушивание
       CloseSocket(listenSocket1, F.port1);
       CloseSocket(listenSocket, F.port);
    }

    private void CloseSocket(Socket s, int port) {
      if(port>0 && s != null) {
         try { s.Shutdown(SocketShutdown.Both); } catch (Exception) { }
         s.Close();
      }
    }

    // Головной модуль запуска задачи https-сервера
    public void StartAccept() {
       while (F.notExit) {
          F.maxNumberAcceptedClients.WaitOne();
          if (F.notExit) {
            if(F.freeClientsPool.TryPop(out int j)) {
              t[j] = F.session[j].AcceptAsync(listenSocket.AcceptAsync(),"https");
            } else {
              F.log2(mess+F.st+".");
            }
          }
       }
    }

    // Головной модуль запуска задачи http-сервера
    public void StartAccept1() {
       while (F.notExit) {
          F.maxNumberAcceptedClients1.WaitOne();
          if (F.notExit) {
            if(F.freeClientsPool1.TryPop(out int j)) {
              t[j] = F.session[j].AcceptAsync(listenSocket1.AcceptAsync(),"http");
            } else {
              F.log2(mess+F.st+".");
            }
          }
       }
    }

  }
}
