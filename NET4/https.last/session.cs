//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                         !!
//!!    https.net сервер на C#.      Автор: A.Б.Корниенко    !!
//!!    class Session                версия от 06.04.2026    !!
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
           fullprg, x1;
    Queue<string> heads = new Queue<string>();
    byte[] buf = new byte[f.bu];
    SslStream stream;
    FileStream file1;           // Файл для записи POST-данных
    Socket client;              // Сокет клиента
    IPEndPoint point;           // IP адрес клиента
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
      if(filename.Length>f.i0) {
        if(Directory.Exists(dirname)) Directory.Delete(dirname,true);
        dirname=filename= string.Empty;
      }

      // Если клиентов много, то сбрасываем счетчики DoS-атак, только если другой IP.
      // А если клиентов больше нет, то сбрасывает счетчик DoS-атаки f.iIP1.
      if(f.nClients>f.i1) {
        Interlocked.Decrement(ref f.nClients);
        if(f.IP != IP) {
          Interlocked.Exchange(ref f.iIP,f.i0);
          Interlocked.Exchange(ref f.iIP1,f.i0);
        }
      } else {
        if(f.IP != IP) Interlocked.Exchange(ref f.iIP,f.i0);
        Interlocked.Exchange(ref f.nClients,f.i0);
        Interlocked.Exchange(ref f.iIP1,f.i0);
      }

      head=h1=res=reso=Host=Content_T=Content_Type=Content_Disposition=QUERY_STRING=string.Empty;
      UTF = Encoding.GetEncoding(f.UTF8);
      eof = len = i2 = Content_Length = f.i0;
      k = k1 = n1 = f.bu1;                  // Смещение для чтения 1-ой части заголовков
      f.IP = f.IP1 = IP;                    // Предыдущий IP для сравнения на выходе и входе
      fs = file1 = null;                    // Освободить объекты
      R = R1 = f.b0;                        // Однобайтовые флажки
      heads.Clear();                        // Очистка блока заголовков
      nbuf = f.bu4;                         // Число читаемых за один раз в заголовках байтов
      n2 = f.bu3;                           // Смещение для чтения 2-ой части заголовков
      l = true;                             // Заголовок пока не прочитан
    }

    // Увеличить счетчик IP с подозрительными запросами
    void fIP() {
      if(f.IP==IP) {
        Interlocked.Increment(ref f.iIP);
        if(f.iIP>f.i2) res+="-";
      }
    }

    public async Task AcceptAsync(Task<Socket> tClient) {
      client = await tClient;
      dt1 = DateTime.UtcNow;
      point = client.RemoteEndPoint as IPEndPoint;
      IP = point.Address.ToString();
      if((f.iIP>f.st1 && f.IP==IP) || (f.iIP1>f.qu1 && f.IP1==IP)) {
        clientClose();
      } else {
        Interlocked.Increment(ref f.nClients);
        if(f.IP1==IP) Interlocked.Increment(ref f.iIP1);
        try{
          stream = new SslStream(new NetworkStream(client,true),false);
          if (! stream.AuthenticateAsServerAsync(f.cert,false,
              (System.Security.Authentication.SslProtocols)12288 |
               System.Security.Authentication.SslProtocols.Tls12,
              false).Wait(2000)) {
            stream.Dispose();
            stream = null;

            // Вероятно обращение не по домену, а по IP. На первый раз пропускаем,
            // но счетчик у этого IP увеличиваем.
            fIP();

          }
        }catch(Exception){
          if(stream != null){
            stream.Dispose();
            stream = null;
          }
        }
        if(stream != null) {
          if(f.IP1==IP) Interlocked.Decrement(ref f.iIP1);

          // Чтение заголовков
          sRead();                   // Читаем во вторую четверть буфера
          while (l) {
            sReadAsync();            // Читаем асинхронно
            if(i>f.i0) {
              len += i;
              getHeaders();
            } else {
              l = false;
            }
          }

          // Заголовки прочитали, фомируем ответ
          if(R>f.b0 && eof==f.i0) {
            n1 = f.i0;
            n2 = f.bu2;
            nbuf = f.bu8;
            if(R>f.b1) {
              putHead(true);
              if(R1>f.b0 || File.Exists(res)) {
                x1 = f.valStr(ref Content_Type,"charset");
                if(x1.Length>f.i0 && !String.Equals(x1,f.UTF8,
                      StringComparison.CurrentCultureIgnoreCase)) {
                  try { UTF = Encoding.GetEncoding(x1); } catch(Exception) { }
                }
                if(R==f.b2) {
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
                  res = f.DocumentRoot+f.DI;
                  if(!gzExists(false)) {
                    putHead(false);
                    if(!File.Exists(res)) {
                      R = f.b0;
                      failure("404 Not Found");
                    }
                  }
                }
              }
              if(R==f.b1) type();
            }
          } else {
            if(res.Length>f.i0) {
              res+=" -";
              failure("403 Forbidden");

              // На первый раз пропускаем, но счетчик у этого IP увеличиваем.
              fIP();

            }
          }
          stream.Close();
        }
        clientClose();

        if(res.Length>f.i1 && f.log9>f.i0) {
          n = DateTime.UtcNow.Subtract(dt1).TotalMilliseconds;
          f.log2("/"+(n>9999?"****" : n.ToString("0000"))+" "+IP+" "+jt+"\t"+res);
        }

        Init();
      }

      // Освободить индекс клиента и сделать его доступным
      f.freeSession(j);
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
           (CT? Content_T : f.CT+": text/html\r\n");
    }

    void putCT(ref string c, string x) {
      c = f.CT+": "+x+"\r\n";
      h1 = f.CC;
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
      if(len>f.i0) {
        i = Array.IndexOf(buf,f.b10,k,len);
        if(i >= f.i0) {
          if(i>f.i0 && buf[i-f.i1]==f.b13) {
            m1 = i-k-f.i1;
            len -= m1+f.i2;
          } else {
            m1 = i-k;
            len -= m1+f.i1;
          }
          z += UTF.GetString(buf,k,m1);
          k = i+f.i1;
        }
      }
      l = z.Length>f.i0;
      return z;
    }

    void getHeaders() {
      string lin,z,h;
      do {
        lin = line1();
// f.log2(" "+lin);
        h = f.afterStr1(ref lin,":");
        h = f.ltri(ref h);
        if(h.Length>f.i0) {
          z = f.beforStr1(ref lin,":");
          switch(z) {
          case "Host":
            Host = h;
            prepResource();
            switch (R) {
            case f.b0:
            case f.b1:
              l = false;  // Дальше читать бессмысленно
              break;
            case f.b2:
              m = -f.i1;
              if(f.cgia) {
                try{
                  m = f.freeCGI.Pop();
                  ts = f.start_CGI(m);
                } catch(Exception) { }
              }
              break;
            case f.b3:
              m = -f.i1;
              if(f.vfpa != null) {
                try{
                  m = f.freeVFP.Pop();
                  ts = f.start_VFP(m);
                } catch(Exception) { }
              }
              break;
            }
            break;
          case f.CT:
            Content_Type = h;
            break;
          case f.CD:
            Content_Disposition = h;
            break;
          case f.CL:
            try { Content_Length = int.Parse(h); } catch(Exception) { Content_Length = f.i0; }
            break;
          }
          heads.Enqueue(z);
          heads.Enqueue(h);
        } else {
          i = lin.IndexOf(" ");
          if(i > f.i0) {
            z = lin.Substring(f.i0,i);
            if(z=="GET" || z=="POST" || z=="PUT") {
              h = lin.Substring(i+f.i1);
              h = f.ltri(ref h);
              i = h.IndexOf(" ");
              if(i > f.i0) reso = h.Substring(f.i0,i);
            }
          }
        }
      } while(l);

      // Перенести остаток байт заголовочной части из bu2 в конец bu1
      if(R>f.b1) {
        i = k;
        k = k1-len;
        Array.Copy(buf, i, buf, k, len);
      }
    }

    void prepResource() {
      string sub,ext = ".";
      if(reso.Length==f.i0) {
        R=f.b0;
      } else {
        res = HttpUtility.UrlDecode(reso);
        QUERY_STRING = f.afterStr1(ref res,"?");
        res = f.beforStr1(ref res,"?");
        sub = f.beforStr1(ref Host,":");

        // ".." в запроах недопустимы в целях безопасности
        if(res.IndexOf("..")<f.i0){

          if(res.EndsWith("/")) res += f.DirectoryIndex;
          reso = f.afterStr9(ref res,"/");
          ext = f.afterStr9(ref reso,ext);
          if(ext.Length==f.i0){
            reso = f.DocumentRoot+sub+res+".";
            if(File.Exists(reso+f.Ext)) {
              R1 = f.b1;      // Случай API
              ext = f.Ext;
              res += "."+ext;
            } else if(File.Exists(reso+"prg")) {
              R1 = f.b1;      // Случай API
              ext = "prg";
            } else if(Directory.Exists(reso)) {
              res += "/"+f.DirectoryIndex;
              ext = f.afterStr9(ref f.DirectoryIndex,".");
            } else if(! File.Exists(reso)) {
              ext = "html";
              res += "."+ext;
            }
          }
        }
        R = f.b1;
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
          Content_T = f.CT_T;
          break;
        default:
          if(ext==f.Ext) {
            R = f.b2;
          } else if(ext=="prg") {
            R = f.b3;
          } else {
            // Все другие расширения недопустимы в целях безопасности
            R = f.b0;
          }
          break;
        }
        reso = sub+res;
        res = f.DocumentRoot+reso;
      }
    }

    void failure(string s) {
      string z = f.H1+s+"\r\n";
      i = UTF.GetBytes(z,f.i0,z.Length,buf,f.i0);
      stream.Write(buf,f.i0,i);
    }

    // Чтение данных синхронно с ожиданием f.tw мс
    void sRead() {
      try {
         ti = stream.ReadAsync(buf, k1, nbuf);
       } catch(Exception) {
         l = false;                            // достигнут конец потока
         eof = -f.i1;
       }
    }

    // Запись данных POST синхронно
    void sWrite(byte b) {
      switch(b) {
      case f.b2:
        f.proc[m].StandardInput.BaseStream.Write(buf,k,i);
        break;
      case f.b3:

        // Только кодировка f.vfpw формирует строку без кодирования
        f.vfp[m].SetVar("__IO",f.vfpw.GetString(buf,k,i));

        f.vfp[m].DoCmd("STD_IO.Write(__IO)");
        break;
      default:
        file1.Write(buf,k,i);      // Пишем синхронно
        break;
      }
    }

    // Асинхронное Чтение данных в половинку буфера
    async void sReadAsync() {
      k1 = k1<f.bu2? n2 : n1;                     // чередуем каке-то буферы в половинках
      try {
        if(ti.Wait(f.tw)){
          i = await ti;
          ti = stream.ReadAsync(buf, k1, nbuf);   // минимальный размер буфера из всех половинок
        } else {
          i = -f.i1;
        }
      } catch(Exception) {
        l = false;                                // достигнут конец потока
        eof = -f.i1;
      }
    }

    // Отправка файла
    void type(){
      head = f.OK+head+f.CL+": ";
      fs = File.OpenRead(res);
      nf = fs.Length;
      head += nf+"\r\n\r\n";
      i = UTF.GetBytes(head, f.i0, head.Length, buf, n1);
      i2 = fs.Read(buf, i, nbuf-i);            // Заполнить первую половину буфера синхронно
      t = stream.WriteAsync(buf, n1, i2+i);    // Асинхронно записать в поток
      k = n2;
      while (i2<nf) {
        i = fs.Read(buf, k, nbuf);             // Синхронно прочитать
        if(i>f.i0) {
          t.Wait();
          t = stream.WriteAsync(buf, k, i);
          k = k==n1? n2 : n1;
          i2 += i;
        } else {
          i2 = (int)nf;
        }
      }
      t.Wait();
      fs.Close();
    }

    bool filename2(){
      filename=f.valStr(ref Content_Disposition,"filename");
      if(filename.Length>f.i0 || Content_Length>f.post){
        dirname=f.DirectorySessions+"/"+IP+"_"+point.Port.ToString();
        if(filename.Length==f.i0) filename=DateTime.Now.ToString("HHmmssfff");
        filename = dirname+"/"+HttpUtility.UrlDecode(filename);
        return true;
      }
      return false;
    }

    // Передаем блок заголовков
    void res_start(){
      reso = res+"\nSCRIPT_FILENAME:"+f.fullres(ref res)+"\nQUERY_STRING:"+
             QUERY_STRING+"\nREMOTE_ADDR:"+IP;
      while (heads.Count>f.i1) reso += "\n"+heads.Dequeue()+":"+heads.Dequeue();
      f.proc[m].StandardInput.WriteLine(reso.Length.ToString()+"\n"+reso);
    }

    // Передача данных из потока в объект
    void send_stream(byte b) {
      if(len<Content_Length) {
        l = true;
        while (l) {

          // Читаем асинхронно, первый буфер был прочитан при чтении заголовков
          sReadAsync();

          if(i>f.i0) {
            i += len;
            i2 += i;
            sWrite(b);  // Пишем синхронно
            l = i2<Content_Length;
            len = f.i0;
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
    void send_file() {

      // Открыть файл, если он не открыт
      if (File.Exists(filename)) {
        File.Delete(filename);
      } else if(!Directory.Exists(dirname)) {
        Directory.CreateDirectory(dirname);
      }
      file1 = new FileStream(filename,FileMode.Create);
      send_stream(f.i0);
      if(file1.CanRead) file1.Close();
    }

    async Task send_cgi() {
      fullprg = f.fullres(ref res);
      if(m < f.i0) {

        // Вывести сообщение об отсутствии интерпретатора
        send_prg1("There is no \""+f.Proc+"\" on the server :(");
        return;
      }

      try{
        if(await ts) m = f.db;
      } catch(Exception) {
        m = f.db;
      }
      if(m >= f.db) {

        // Вывести сообщение, что все доступные процессы интерпретатора заняты
        send_prg1("All "+f.db.ToString()+" \""+f.Proc+"\" processes are busy :(");
        return;
      }

      // Чтение данных POST
      heads.Enqueue("POST_FILENAME");
      if(filename2()) {

        // Если в потоке файл
        heads.Enqueue(f.Folder+filename);
        send_file();
        res_start();

      } else {

        // и если просто поток
        heads.Enqueue(filename);
        res_start();
        send_stream(R);
      }
      f.proc[m].StandardInput.Close();

      if(eof==f.i0) {      // Если нет разрыва связи

        // Вывод полученных данных cgi-скрипта
        reso = f.OK+head;

        // Помещаем заголовок в буфер с позиции n2
        k = UTF.GetBytes(reso, f.i0, reso.Length, buf, n2);

        i1 = nbuf-k;    // До конца буфера осталось

        // Прочитать i1 символов в buf начиная с n2+k
        k1 = n2+k;
        i1 = f.proc[m].StandardOutput.BaseStream.Read(buf, k1, i1);

        // Проверить код возврата
        if(R1>f.b0) {
          i=f.valInt(UTF.GetString(buf, k1, 4));
          if(i>=100 && i<=599) {               // Случай API
             i = Array.IndexOf(buf, f.b10, k1, i1);
             if(i>k1) {
                i++;
                reso = f.H1+UTF.GetString(buf,k1,i-k1)+head;
                k1 = i-UTF.GetByteCount(reso);
                UTF.GetBytes(reso,f.i0,reso.Length,buf,k1);
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
        while (i1>f.i0) {
          t = stream.WriteAsync(buf, k1, i1);  // Асинхронно записать в поток
          k1 = k1<n2? n2 : n1;                 // Следующее начало буфера
          i1 = f.proc[m].StandardOutput.BaseStream.Read(buf, k1, nbuf);
          t.Wait();
        }
      }
      await f.clear_cgi(m);
    }

    // Вывод текстового сообщения длиной до 1 буфера
    void send_prg1(string s) {
      string z = f.OK+head+f.CT_T+"\r\n"+s;
      i = UTF.GetBytes(z,f.i0,z.Length,buf,f.i0);
      stream.Write(buf,f.i0,i);
    }

    // Прочитать i1 символов начиная с i2 в buf начиная с k1
    void stdioRead() {
      i2 += i1;            // Превести позицию в STD_IO
      if(i2>Content_Length) i1 -= i2-Content_Length;

      // Файлы выводятся только с таким кодированием
      f.vfpw.GetBytes(f.vfp[m].Eval("STD_IO.Read("+i1+")"), f.i0, i1, buf, k1);
    }

    async Task send_prg() {
      prg=f.afterStr9(ref res,"/");
      fullprg=f.fullres(ref res);
      if(m < f.i0) {

        // Вывести сообщение об отсутствии VFP в реестре
        send_prg1("MS VFP is missing in the Windows registry :(");
        return;
      }

      if(await ts) {
        try{ f.start_VFP3(m); }
        catch(Exception) { m = f.db; }
      }
      if(m >= f.db) {

        // Вывести сообщение, что все процессы VFP заняты
        send_prg1("All "+f.db.ToString()+" VFP processes are busy :(");
        return;

      } else {
        f.vfp[m].DoCmd("SET DEFA TO (\""+f.beforStr9(ref fullprg,"/")+"\")");
        f.vfp[m].SetVar("QUERY_STRING",QUERY_STRING);
        f.vfp[m].SetVar("SCRIPT_FILENAME",fullprg);
        f.vfp[m].SetVar("REMOTE_ADDR",IP);
        while (heads.Count>f.i1) f.vfp[m].SetVar("_"+heads.Dequeue().Replace("-","_")+
              "_",heads.Dequeue());
        if(filename2()) {     // Определяем и проверяем наличие имя файла для POST-данных
          f.vfp[m].SetVar("POST_FILENAME",f.Folder+filename);
          send_file();        // Записываем в файл
        } else {
          f.vfp[m].SetVar("POST_FILENAME",filename);
          send_stream(R);     // Записываем в STD_IO в VFP
        }
        if(eof < f.i0) {         // Если обнаружен разрыв связи
          f.clear_prg(m);
          return;
        }
      }

      // Вывод полученных данных prg-скрипта
      try{
        head = f.OK+head;
        if(R1==f.b0){

          // Если выполнение prg не закончилось за 25 минут, то аварийно снять процесс
          if(! Task.Run(() => f.vfp[m].Eval(f.beforStr9(ref prg,".prg")+"()")).Wait(f.i8))
             f.killVFP(m);

        }else{      // Случай API
          var api= Task.Run(() => f.vfp[m].Eval(f.beforStr9(ref prg,".prg")+"()"));
          if(api.Wait(f.i8)) {
            var ret = await api;
            if(ret.GetType().Name=="String")
               if(ret.Length>5) {
                 i = f.valInt(ret.Substring(f.i0,4));
                 if(i>=100 && i<=599) head = f.H1+ret+"\r\n";
            }
          } else {
            f.killVFP(m);
          }
        }
        Content_Length = f.vfp[m].Eval("STD_IO.LenStream()");
      }catch(Exception e){
        head=f.OK+head+f.CT_T+"\r\nError in VFP: "+e.Message;
        Content_Length = f.i0;
      }

      // Начальная позиция в STD_IO
      i2 = f.i0;

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
        t.Wait();

        // Асинхронно записать в поток, сконвертировав кодировку
        t = stream.WriteAsync(buf, k1, i1);

      }
      t.Wait();
      f.clear_prg(m);
    }

    // Завершить ожидание
    public void Stop() {
       if(client != null) {
         try { client.Shutdown(SocketShutdown.Both); } catch (Exception) { }
         client.Close();
       }

      // Освободить индекс клиента и сделать его доступным
      f.freeSession(j);
    }
  }
}
