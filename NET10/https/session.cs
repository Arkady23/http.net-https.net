//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                         !!
//!!    https.net сервер на C#.      Автор: A.Б.Корниенко    !!
//!!    class Session                версия от 20.05.2026    !!
//!!                                                         !!
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

using System;
using System.IO;
using System.Web;
using System.Net;
using System.Text;
using System.Threading;
using System.Net.Sockets;
using System.Diagnostics;
using System.Net.Security;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace https2 {

  public class Session {
    int i, j, k, m, m1, i1, i2, k1, len, eof, Content_Length, n1, n2, nbuf;
    string h1, reso, res, head, Host, Content_Type, Content_T, IP, jt,
           Content_Disposition, QUERY_STRING, dirname, filename, prg,
           fullprg, Protocol, x1;
    Queue<string> heads = new Queue<string>();
    byte[] buf = new byte[F.bu];
    SslStream sslStream;
    FileStream file1;           // Файл для записи POST-данных
    IPEndPoint point;           // IP адрес клиента
    Stream stream;              // Объявляем объект как базовый Stream
    Socket client;              // Сокет клиента
    FileStream fs;              // Файл статического ресурса
    Encoding UTF;
    DateTime dt1;
    byte R, R1;
    double n;
    long nf;                    // Длина посылаемого файла/потока
    bool l;                     // false, если заголовки прочитаны
    Task t;                     // Задачи ввода-вывода
    Task<int> ti;               // Задача контроля ожидания
    Task<bool> ts;              // Задача запуска обработчика

    public Session(int j) {
      jt = (j).ToString();
      dirname=filename= string.Empty;
      this.Init();
      this.j = j;
    }

    void Init() {

      // Подготовка переменных по максимуму
      if(filename.Length>F.i0) {
        if(Directory.Exists(dirname)) Directory.Delete(dirname,true);
        dirname=filename= string.Empty;
      }

      // Если клиентов много, то сбрасываем счетчики DoS-атак, только если другой IP.
      // А если клиентов больше нет, то сбрасывает счетчик DoS-атаки F.iIP1.
      if(F.nClients>F.i1) {
        Interlocked.Decrement(ref F.nClients);
        if(F.IP != IP) {
          Interlocked.Exchange(ref F.iIP,F.i0);
          Interlocked.Exchange(ref F.iIP1,F.i0);
        }
      } else {
        if(F.IP != IP) Interlocked.Exchange(ref F.iIP,F.i0);
        Interlocked.Exchange(ref F.nClients,F.i0);
        Interlocked.Exchange(ref F.iIP1,F.i0);
      }

      head=h1=res=reso=Host=Content_T=Content_Type=Content_Disposition=QUERY_STRING=string.Empty;
      UTF = Encoding.GetEncoding(F.UTF8);
      eof = len = i2 = Content_Length = F.i0;
      k = k1 = n1 = F.bu1;                  // Смещение для чтения 1-ой части заголовков
      F.IP = F.IP1 = IP;                    // Предыдущий IP для сравнения на выходе и входе
      fs = file1 = null;                    // Освободить объекты
      R = R1 = F.b0;                        // Однобайтовые флажки
      heads.Clear();                        // Очистка блока заголовков
      nbuf = F.bu4;                         // Число читаемых за один раз в заголовках байтов
      n2 = F.bu3;                           // Смещение для чтения 2-ой части заголовков
      l = true;                             // Заголовок пока не прочитан
    }

    // Увеличить счетчик IP с подозрительными запросами
    void fIP() {
      if(F.IP==IP) {
        Interlocked.Increment(ref F.iIP);
        if(F.iIP>F.i2) res+="-";
      }
    }

    public async Task AcceptAsync(Task<Socket> tClient, string Prot) {
      client = await tClient;
      dt1 = DateTime.UtcNow;
      point = client.RemoteEndPoint as IPEndPoint;
      IP = point.Address.ToString();
      Protocol = Prot;
      if((F.iIP>F.st1 && F.IP==IP) || (F.iIP1>F.qu1 && F.IP1==IP)) {
        clientClose();
      } else {
        Interlocked.Increment(ref F.nClients);
        if(F.IP1==IP) Interlocked.Increment(ref F.iIP1);

        if(Prot=="https") {
          using var cts = new CancellationTokenSource(2000);
          try{
            sslStream = new SslStream(new NetworkStream(client, true), false);
            await sslStream.AuthenticateAsServerAsync(F.cert, cts.Token);
            stream = sslStream;
          }catch (OperationCanceledException){
            stream?.Dispose();
            client?.Dispose();
            stream = null;
            fIP();
          }catch(Exception){
            stream?.Dispose();
            client?.Dispose();
            stream = null;
          }
        }else{
          try {
            stream = new NetworkStream(client,true);
          } catch(Exception) {
            stream?.Dispose();
            stream = null;
          }
        }
        if(stream != null) {
          if(F.IP1==IP) Interlocked.Decrement(ref F.iIP1);

          // Чтение заголовков
          sRead();                   // Читаем во вторую четверть буфера
          while (l) {
            await sReadAsync();      // Читаем асинхронно
            if(i>F.i0) {
              len += i;
              getHeaders();
            } else {
              l = false;
            }
          }

          // Заголовки прочитали, фомируем ответ
          if(R>F.b0 && eof==F.i0) {
            n1 = F.i0;
            n2 = F.bu2;
            nbuf = F.bu8;
            if(R>F.b1) {
              putHead(true);
              if(R1>F.b0 || File.Exists(res)) {
                x1 = F.valStr(ref Content_Type,"charset");
                if(x1.Length>F.i0 && !String.Equals(x1,F.UTF8,
                      StringComparison.CurrentCultureIgnoreCase)) {
                  try { UTF = Encoding.GetEncoding(x1); } catch(Exception) { }
                }
                if(R==F.b2) {
                  await send_cgi();
                } else {
                  await send_prg();
                }
              }
            } else {
              if(!gzExists(true)) {
                if(File.Exists(res)) {
                  putHead(true);
                } else {
                  res = F.DocumentRoot+F.DI;
                  if(!gzExists(false)) {
                    putHead(false);
                    if(!File.Exists(res)) {
                      R = F.b0;
                      failure("404 Not Found");
                    }
                  }
                }
              }
              if(R==F.b1) await typeAsync();
            }
          } else {
            if(res.Length>F.i0) {
              res+=" -";
              failure("403 Forbidden");

              // На первый раз пропускаем, но счетчик у этого IP увеличиваем.
              fIP();

            }
          }
          stream.Close();
        }
        clientClose();

        if(res.Length>F.i1 && F.log9>F.i0) {
          n = DateTime.UtcNow.Subtract(dt1).TotalMilliseconds;
          F.log2("/"+(n>9999?"****" : n.ToString("0000"))+" "+IP+" "+jt+"\t"+res);
        }

        Init();
      }

      // Освободить индекс клиента и сделать его доступным
      F.freeSession(j, Protocol);
    }

    // close the socket associated with the client
    void clientClose() {
      try { client.Shutdown(SocketShutdown.Send); } catch (Exception) { }
      client.Close();
    }

    void putHead(bool CT) {
      // CT - true, тип контента не изменяется
      //      false, тип контента стал html.
      head="Date: "+dt1.ToString("R")+"\r\n"+h1+
           (CT? Content_T : F.CT+": text/html\r\n");
    }

    void putCT(ref string c, string x) {
      c = F.CT+": "+x+"\r\n";
      h1 = F.CC;
    }

    bool gzExists(bool CT) {
      string gz=res+".gz";
      bool l = File.Exists(gz);
      if( l ) {
        res = gz;
        putHead(CT);
        head += "Content-Encoding: gzip\r\n";
      }
      return l;
    }

    string line1() {
      string z = string.Empty;
      if(len>F.i0) {
        i = Array.IndexOf(buf,F.b10,k,len);
        if(i >= F.i0) {
          if(i>F.i0 && buf[i-F.i1]==F.b13) {
            m1 = i-k-F.i1;
            len -= m1+F.i2;
          } else {
            m1 = i-k;
            len -= m1+F.i1;
          }
          z += UTF.GetString(buf,k,m1);
          k = i+F.i1;
        }
      }
      l = z.Length>F.i0;
      return z;
    }

    void getHeaders() {
      string lin,z,h;
      do {
        lin = line1();
// F.log2(" "+lin);
        h = F.afterStr1(ref lin,":");
        h = F.ltri(ref h);
        if(h.Length>F.i0) {
          z = F.beforStr1(ref lin,":");
          switch(z) {
          case "Host":
            Host = h;
            prepResource();
            switch (R) {
            case F.b0:
            case F.b1:
              l = false;  // Дальше читать бессмысленно
              break;
            case F.b2:
              m = -F.i1;
              if(F.cgia) {
                try{
                  m = F.freeCGI.Pop();
                  ts = F.start_CGI(m);
                } catch(Exception) { }
              }
              break;
            case F.b3:
              m = -F.i1;
              if(F.vfpa != null) {
                try{
                  m = F.freeVFP.Pop();
                  ts = F.start_VFP(m);
                } catch(Exception) { }
              }
              break;
            }
            break;
          case F.CT:
            Content_Type = h;
            break;
          case F.CD:
            Content_Disposition = h;
            break;
          case F.CL:
            try { Content_Length = int.Parse(h); } catch(Exception) { Content_Length = F.i0; }
            break;
          }
          heads.Enqueue(z);
          heads.Enqueue(h);
        } else {
          i = lin.IndexOf(" ");
          if(i > F.i0) {
            z = lin.Substring(F.i0,i);
            if(z=="GET" || z=="POST" || z=="PUT") {
              h = lin.Substring(i+F.i1);
              h = F.ltri(ref h);
              i = h.IndexOf(" ");
              if(i > F.i0) reso = h.Substring(F.i0,i);
            }
          }
        }
      } while(l);

      // Перенести остаток байт заголовочной части из bu2 в конец bu1
      if(R>F.b1) {
        i = k;
        k = k1-len;
        Array.Copy(buf, i, buf, k, len);
      }
    }

    void prepResource() {
      string sub,ext = ".";
      if(reso.Length==F.i0) {
        R=F.b0;
      } else {
        res = HttpUtility.UrlDecode(reso);
        QUERY_STRING = F.afterStr1(ref res,"?");
        res = F.beforStr1(ref res,"?");
        sub = F.beforStr1(ref Host,":");

        // ".." в запроах недопустимы в целях безопасности
        if(res.IndexOf("..")<F.i0){

          if(res.EndsWith("/")) res += F.DirectoryIndex;
          reso = F.afterStr9(ref res,"/");
          ext = F.afterStr9(ref reso,ext);
          if(ext.Length==F.i0){
            reso = F.DocumentRoot+sub+res+".";
            if(File.Exists(reso+F.Ext)) {
              R1 = F.b1;      // Случай API
              ext = F.Ext;
              res += "."+ext;
            } else if(File.Exists(reso+"prg")) {
              R1 = F.b1;      // Случай API
              ext = "prg";
            } else if(Directory.Exists(reso)) {
              res += "/"+F.DirectoryIndex;
              ext = F.afterStr9(ref F.DirectoryIndex,".");
            } else if(! File.Exists(reso)) {
              ext = "html";
              res += "."+ext;
            }
          }
        }
        R = F.b1;
        switch(ext) {
        case "html":
          putCT(ref Content_T,"text/html");
          break;
        case "svg":
          putCT(ref Content_T,"image/svg+xml");
          break;
        case "gif":
          putCT(ref Content_T,"image/gif");
          break;
        case "png":
          putCT(ref Content_T,"image/png");
          break;
        case "jpeg":
        case "jpg":
          putCT(ref Content_T,"image/jpeg");
          break;
        case "js":
          putCT(ref Content_T,"text/javascript");
          break;
        case "css":
          putCT(ref Content_T,"text/css");
          break;
        case "ico":
          putCT(ref Content_T,"image/x-icon");
          break;
        case "mp4":
          putCT(ref Content_T,"video/mp4");
          break;
        case "txt":
        case "":
          Content_T = F.CT_T;
          break;
        default:
          if(ext==F.Ext) {
            R = F.b2;
          } else if(ext=="prg") {
            R = F.b3;
          } else {
            // Все другие расширения недопустимы в целях безопасности
            R = F.b0;
          }
          break;
        }
        reso = sub+res;
        res = F.DocumentRoot+reso;
      }
    }

    void failure(string s) {
      string z = F.H1+s+"\r\n";
      i = UTF.GetBytes(z,F.i0,z.Length,buf,F.i0);
      stream.Write(buf,F.i0,i);
    }

    // Чтение данных синхронно с ожиданием F.tw мс
    void sRead() {
      try {
         ti = stream.ReadAsync(buf, k1, nbuf);
       } catch(Exception) {
         l = false;                            // достигнут конец потока
         eof = -F.i1;
       }
    }

    // Запись данных POST синхронно
    void sWrite(byte b) {
      switch(b) {
      case F.b2:
        F.proc[m].StandardInput.BaseStream.Write(buf,k,i);
        break;
      case F.b3:

        // Только кодировка F.vfpw формирует строку без кодирования
        F.vfp[m].SetVar("__IO",F.vfpw.GetString(buf,k,i));

        F.vfp[m].DoCmd("STD_IO.Write(__IO)");
        break;
      default:
        file1.Write(buf,k,i);      // Пишем синхронно
        break;
      }
    }

    // Асинхронное Чтение данных в половинку буфера
    async Task sReadAsync() {
      using var cts = new CancellationTokenSource(F.tw);
      k1 = k1<F.bu2? n2 : n1;                  // чередуем каке-то буферы в половинках
      try {
        i = await ti.WaitAsync(cts.Token);
        ti = stream.ReadAsync(buf, k1, nbuf);  // минимальный размер буфера из всех половинок
      } catch(OperationCanceledException) {
        i = -F.i1;
      } catch(Exception) {
        l = false;                             // достигнут конец потока
        eof = -F.i1;
      }
    }

    // Отправка файла
    async Task typeAsync(){
      head = F.OK+head+F.CL+": ";
      fs = File.OpenRead(res);
      nf = fs.Length;
      head += nf+"\r\n\r\n";
      i = UTF.GetBytes(head, F.i0, head.Length, buf, n1);
      i2 = fs.Read(buf, i, nbuf-i);            // Заполнить первую половину буфера синхронно
      t = stream.WriteAsync(buf, n1, i2+i);    // Асинхронно записать в поток
      k = n2;
      while (i2<nf) {
        i = fs.Read(buf, k, nbuf);             // Синхронно прочитать
        if(i>F.i0) {
          await t;
          t = stream.WriteAsync(buf, k, i);
          k = k==n1? n2 : n1;
          i2 += i;
        } else {
          i2 = (int)nf;
        }
      }
      await t;
      fs.Close();
    }

    bool filename2(){
      filename=F.valStr(ref Content_Disposition,"filename");
      if(filename.Length>F.i0 || Content_Length>F.post){
        dirname=F.DirectorySessions+"/"+IP+"_"+point.Port.ToString();
        if(filename.Length==F.i0) filename=DateTime.Now.ToString("HHmmssfff");
        filename = dirname+"/"+HttpUtility.UrlDecode(filename);
        return true;
      }
      return false;
    }

    // Передаем блок заголовков
    void res_start(){
      reso = res+"\nSCRIPT_FILENAME:"+F.fullres(ref res)+"\nQUERY_STRING:"+
             QUERY_STRING+"\nREMOTE_ADDR:"+IP+"\nSERVER_PROTOCOL:"+Protocol;
      while (heads.Count>F.i1) reso += "\n"+heads.Dequeue()+":"+heads.Dequeue();
      F.proc[m].StandardInput.WriteLine(reso.Length.ToString()+"\n"+reso);
    }

    // Передача данных из потока в объект
    async Task send_stream(byte b) {
      if(len<Content_Length) {
        l = true;
        while (l) {

          // Читаем асинхронно, первый буфер был прочитан при чтении заголовков
          await sReadAsync();

          if(i>F.i0) {
            i += len;
            i2 += i;
            sWrite(b);  // Пишем синхронно
            l = i2<Content_Length;
            len = F.i0;
            k = k1;
          } else {
            l = false;
          }
        }
      } else {
        i = len;
        sWrite(b);      // Пишем синхронно
      }
    }

    // Чтение файла из трафика
    async Task send_file() {

      // Открыть файл, если он не открыт
      if (File.Exists(filename)) {
        File.Delete(filename);
      } else if(!Directory.Exists(dirname)) {
        Directory.CreateDirectory(dirname);
      }
      file1 = new FileStream(filename,FileMode.Create);
      await send_stream(F.i0);
      if(file1.CanRead) file1.Close();
    }

    async Task send_cgi() {
      fullprg = F.fullres(ref res);
      if(m < F.i0) {

        // Вывести сообщение об отсутствии интерпретатора
        send_prg1("There is no \""+F.Proc+"\" on the server :(");
        return;
      }

      try{
        if(await ts) m = F.db;
      } catch(Exception) {
        m = F.db;
      }
      if(m >= F.db) {

        // Вывести сообщение, что все доступные процессы интерпретатора заняты
        send_prg1("All "+F.db.ToString()+" \""+F.Proc+"\" processes are busy :(");
        return;
      }

      // Чтение данных POST
      heads.Enqueue("POST_FILENAME");
      if(filename2()) {

        // Если в потоке файл
        heads.Enqueue(F.Folder+filename);
        await send_file();
        res_start();

      } else {

        // и если просто поток
        heads.Enqueue(filename);
        res_start();
        await send_stream(R);
      }
      F.proc[m].StandardInput.Close();

      if(eof==F.i0) {      // Если нет разрыва связи

        // Вывод полученных данных cgi-скрипта
        reso = F.OK+head;

        // Помещаем заголовок в буфер с позиции n2
        k = UTF.GetBytes(reso, F.i0, reso.Length, buf, n2);

        i1 = nbuf-k;    // До конца буфера осталось

        // Прочитать i1 символов в buf начиная с n2+k
        k1 = n2+k;
        i1 = F.proc[m].StandardOutput.BaseStream.Read(buf, k1, i1);

        // Проверить код возврата
        if(R1>F.b0) {
          i=F.valInt(UTF.GetString(buf, k1, F.i4));
          if(i>=100 && i<=599) {               // Случай API
             i = Array.IndexOf(buf, F.b10, k1, i1);
             if(i>k1) {
                i++;
                reso = F.H1+UTF.GetString(buf,k1,i-k1)+head;
                k1 = i-UTF.GetByteCount(reso);
                UTF.GetBytes(reso,F.i0,reso.Length,buf,k1);
                i1 += n2-k1;
             } else {
                k1 = n2;
             }
          } else {
            k1 = n2;
          }
        } else {
          k1 = n2;
        }

        i1 += k;
        while (i1>F.i0) {
          t = stream.WriteAsync(buf, k1, i1);  // Асинхронно записать в поток
          k1 = k1<n2? n2 : n1;                 // Следующее начало буфера
          i1 = F.proc[m].StandardOutput.BaseStream.Read(buf, k1, nbuf);
          await t;
        }
      }
      await F.clear_cgi(m);
    }

    // Вывод текстового сообщения длиной до 1 буфера
    void send_prg1(string s) {
      string z = F.OK+head+F.CT_T+"\r\n"+s;
      i = UTF.GetBytes(z,F.i0,z.Length,buf,F.i0);
      stream.Write(buf,F.i0,i);
    }

    // Прочитать i1 символов начиная с i2 в buf начиная с k1
    void stdioRead() {
      i2 += i1;            // Превести позицию в STD_IO
      if(i2>Content_Length) i1 -= i2-Content_Length;

      // Файлы выводятся только с таким кодированием
      F.vfpw.GetBytes(F.vfp[m].Eval("STD_IO.Read("+i1+")"), F.i0, i1, buf, k1);
    }

    async Task send_prg() {
      prg=F.afterStr9(ref res,"/");
      fullprg=F.fullres(ref res);
      if(m < F.i0) {

        // Вывести сообщение об отсутствии VFP в реестре
        send_prg1("MS VFP is missing in the Windows registry :(");
        return;
      }

      if(await ts) {
        try{ F.start_VFP3(m); }
        catch(Exception) { m = F.db; }
      }
      if(m >= F.db) {

        // Вывести сообщение, что все процессы VFP заняты
        send_prg1("All "+F.db.ToString()+" VFP processes are busy :(");
        return;

      } else {
        F.vfp[m].DoCmd("SET DEFA TO (\""+F.beforStr9(ref fullprg,"/")+"\")");
        F.vfp[m].SetVar("QUERY_STRING",QUERY_STRING);
        F.vfp[m].SetVar("SERVER_PROTOCOL",Protocol);
        F.vfp[m].SetVar("SCRIPT_FILENAME",fullprg);
        F.vfp[m].SetVar("REMOTE_ADDR",IP);
        while (heads.Count>F.i1) F.vfp[m].SetVar("_"+heads.Dequeue().Replace("-","_")+
              "_",heads.Dequeue());
        if(filename2()) {     // Определяем и проверяем наличие имя файла для POST-данных
          F.vfp[m].SetVar("POST_FILENAME",F.Folder+filename);
          await send_file();        // Записываем в файл
        } else {
          F.vfp[m].SetVar("POST_FILENAME",filename);
          await send_stream(R);  // Записываем в STD_IO в VFP
        }
        if(eof < F.i0) {         // Если обнаружен разрыв связи
          F.clear_prg(m);
          return;
        }
      }

      // Вывод полученных данных prg-скрипта
      using var cts = new CancellationTokenSource(F.i8);
      try{
        head = F.OK+head;
        if(R1==F.b0){

          // Если выполнение prg не закончилось за 25 минут, то аварийно снять процесс
          await Task.Run(() => F.vfp[m].Eval(F.beforStr9(ref prg,".prg")+
                        "()")).WaitAsync(cts.Token);

        }else{      // Случай API
          var api= Task.Run(() => F.vfp[m].Eval(F.beforStr9(ref prg,".prg")+"()"));

          await api.WaitAsync(cts.Token);
          var ret = await api;
          if(ret.GetType().Name=="String")
             if(ret.Length>5) {
               i = F.valInt(ret.Substring(F.i0,F.i4));
               if(i>=100 && i<=599) head = F.H1+ret+"\r\n";
          }
        }
        Content_Length = F.vfp[m].Eval("STD_IO.LenStream()");
      } catch(OperationCanceledException){
        F.killVFP(m);
        head=F.OK+head+F.CT_T+"\r\nError in VFP: The maximum calculation duration of "+
             F.i8+" ms has been exceeded.";
        Content_Length = F.i0;
      } catch(Exception e){
        head=F.OK+head+F.CT_T+"\r\nError in VFP: "+e.Message;
        Content_Length = F.i0;
      }

      // Начальная позиция в STD_IO
      i2 = F.i0;

      // Помещаем заголовок в буфер
      k = UTF.GetBytes(head,i2,head.Length,buf,n2);

      i1 = nbuf-k;                // До конца буфера осталось

      // Прочитать i1 символов начиная с i2 в buf начиная с n2
      k1 = n2+k;
      stdioRead();

      // Асинхронно байты записать в поток
      t = stream.WriteAsync(buf, n2, i1+k);
                           
      i1 = nbuf;
      while (i2<Content_Length) {
        k1 = k1<n2? n2 : n1;      // Следующее начало буфера
        stdioRead();
        await t;

        // Асинхронно записать в поток, сконвертировав кодировку
        t = stream.WriteAsync(buf, k1, i1);

      }
      await t;
      F.clear_prg(m);
    }

    // Завершить ожидание
    public void Stop() {
       if(client != null) {
         try { client.Shutdown(SocketShutdown.Both); } catch (Exception) { }
         client.Close();
       }

      // Освободить индекс клиента и сделать его доступным
      F.freeSession(j, string.Empty);
    }
  }
}
