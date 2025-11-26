//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                         !!
//!!    https.net сервер на C#.      Автор: A.Б.Корниенко    !!
//!!    class Session                версия от 13.06.2025    !!
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
    int i, j, k, m, i1, i2, k1, len, eof, Content_Length, n1, n2, nbuf;
    string h1, reso, res, head, Host, Content_Type, Content_T, IP, jt,
           Content_Disposition, QUERY_STRING, dirname, filename, prg,
           fullprg, x1, x2;
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
    Task<int> ti;               //

    public Session(int j) {
      jt = (j).ToString();
      dirname=filename= "";
      this.Init();
      this.j = j;
    }

    void Init() {

      // Подготовка переменных по максимуму
      if(filename.Length>0) {
        if(Directory.Exists(dirname)) Directory.Delete(dirname,true);
        dirname=filename= "";
      }

      // Если клиентов много, то сбрасываем счетчики DoS-атак, только если другой IP.
      // А если клиентов больше нет, то сбрасывает счетчик DoS-атаки f.iIP1.
      if(f.nClients>1) {
        Interlocked.Decrement(ref f.nClients);
        if(f.IP != IP) {
          Interlocked.Exchange(ref f.iIP,0);
          Interlocked.Exchange(ref f.iIP1,0);
        }
      } else {
        if(f.IP != IP) Interlocked.Exchange(ref f.iIP,0);
        Interlocked.Exchange(ref f.nClients,0);
        Interlocked.Exchange(ref f.iIP1,0);
      }

      head=h1=res=reso=Host=Content_T=Content_Type=Content_Disposition=QUERY_STRING="";
      UTF = Encoding.GetEncoding(f.UTF8);
      eof = len = i2 = Content_Length = 0;
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
      if((f.iIP>f.i2 && f.IP==IP) || (f.iIP1>f.st1 && f.IP1==IP)) {
        clientClose();
      } else {
        Interlocked.Increment(ref f.nClients);
        if(f.IP1==IP) Interlocked.Increment(ref f.iIP1);
        try{
          stream = new SslStream(new NetworkStream(client,true),false);
          if (! stream.AuthenticateAsServerAsync(f.cert,false,
              System.Security.Authentication.SslProtocols.Tls12,false).Wait(200)){
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

          // Чтение заголовков
          sRead();                   // Читаем во вторую четверть буфера
          while (l) {
            sReadAsync();            // Читаем асинхронно
            if(i>0) {
              len += i;
              getHeaders();
            } else {
              l = false;
            }
          }

          // Заголовки прочитали, фомируем ответ
          if(R>f.b0 && eof==0) {
            n1 = 0;
            n2 = f.bu2;
            nbuf = f.bu8;
            head="Date: "+dt1.ToString("R")+"\r\n"+h1+Content_T;
            if(R>1) {
              if(R1>0 || File.Exists(res)) {
                x2 = f.valStr(ref Content_Type,"charset");
                if(x2.Length>0 && !String.Equals(x2,f.UTF8,
                      StringComparison.CurrentCultureIgnoreCase)) {
                  try { UTF = Encoding.GetEncoding(x2); } catch(Exception) { }
                }
                if(R==f.b2) {
                  send_cgi();
                } else {
                  send_prg();
                }
              }
            } else {
              if(!gzExists()) {
                if(!File.Exists(res)) {
                  res = f.DocumentRoot+f.DI;
                  if(!gzExists()) {
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
            if(res.Length>0) {
              res+=" -";
              failure("403 Forbidden");

              // На первый раз пропускаем, но счетчик у этого IP увеличиваем.
              fIP();

            }
          }
          stream.Close();
        }
        clientClose();

        if(res.Length>1 && f.log9>0) {
          n = DateTime.UtcNow.Subtract(dt1).TotalMilliseconds;
          x1 = (n>9999?"****" : n.ToString("0000"));
          f.log2("/"+x1+" "+IP+" "+jt+"\t"+res);
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

    void putCT(ref string c, string x) {
      c = f.CT+": "+x+"\r\n";
    }

    bool gzExists() {
      string gz=res+".gz";
      bool l = File.Exists(gz);
      if( l ) {
       res = gz;
        head += "Content-Encoding: gzip\r\n";
      }
      return l;
    }

    string line1() {
      string z = "";
      if(len>0) {
        i = Array.IndexOf(buf,f.b10,k,len);
        if(i >= 0) {
          if(i>0 && buf[i-1]==f.b13) {
            m = i-k-1;
            len -= m+2;
          } else {
            m = i-k;
            len -= m+1;
          }
          z += UTF.GetString(buf,k,m);
          k = i+1;
        }
      }
      l = z.Length>0;
      return z;
    }

    void getHeaders() {
      string lin,z,h;
      do {
        lin = line1();
// f.log2(" "+lin);
        h = f.afterStr1(ref lin,":");
        h = f.ltri(ref h);
        if(h.Length>0) {
          z = f.beforStr1(ref lin,":");
          switch(z) {
          case "Host":
            Host = h;
            prepResource();
            if(R<f.b2) l = false;  // Дальше читать бессмысленно
            break;
          case f.CT:
            Content_Type = h;
            break;
          case f.CD:
            Content_Disposition = h;
            break;
          case f.CL:
            try { Content_Length = int.Parse(h); } catch(Exception) { Content_Length = 0; }
            break;
          }
          heads.Enqueue(z);
          heads.Enqueue(h);
        } else {
          i = lin.IndexOf(" ");
          if(i > 0) {
            z = lin.Substring(0,i);
            if(z=="GET" || z=="POST" || z=="PUT") {
              h = lin.Substring(i+1);
              h = f.ltri(ref h);
              i = h.IndexOf(" ");
              if(i > 0) reso = h.Substring(0,i);
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
      if(reso.Length==0) {
        R=f.b0;
      } else {
        res = HttpUtility.UrlDecode(reso);
        QUERY_STRING = f.afterStr1(ref res,"?");
        res = f.beforStr1(ref res,"?");
        sub = f.beforStr1(ref Host,":");

        // ".." в запроах недопустимы в целях безопасности
        if(res.IndexOf("..")<0){

          if(res.EndsWith("/")) res += f.DirectoryIndex;
          reso = f.afterStr9(ref res,"/");
          ext = f.afterStr9(ref reso,ext);
          if(ext.Length==0){
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
          h1 = f.CC;
          break;
        case "svg":
          putCT(ref Content_T,"image/svg+xml");
          h1 = f.CC;
          break;
        case "gif":
          putCT(ref Content_T,"image/gif");
          h1 = f.CC;
          break;
        case "png":
          putCT(ref Content_T,"image/png");
          h1 = f.CC;
          break;
        case "jpeg":
        case "jpg":
          putCT(ref Content_T,"image/jpeg");
          h1 = f.CC;
          break;
        case "js":
          putCT(ref Content_T,"text/javascript");
          h1 = f.CC;
          break;
        case "css":
          putCT(ref Content_T,"text/css");
          h1 = f.CC;
          break;
        case "ico":
          putCT(ref Content_T,"image/x-icon");
          h1 = f.CC;
          break;
        case "mp4":
          putCT(ref Content_T,"video/mp4");
          h1 = f.CC;
          break;
        case "":
          Content_T = f.CT_T;
          break;
        default:
          if(ext==f.Ext) {
            R = f.b2;
          } else if(ext=="prg") {
            R = 3;
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
      i = UTF.GetBytes(z,0,z.Length,buf,0);
      stream.Write(buf,0,i);
    }

    // Чтение данных синхронно с ожиданием f.tw мс
    void sRead() {
      try {
         ti = stream.ReadAsync(buf, k1, nbuf);
       } catch(Exception) {
         l = false;                            // достигнут конец потока
         eof = -1;
       }
    }

    // Запись данных POST синхронно
    void sWrite(byte b) {
      switch(b) {
      case 2:
        f.proc[m].StandardInput.BaseStream.Write(buf,k,i);
        break;
      case 3:

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
          i = -1;
        }
      } catch(Exception) {
        l = false;                                // достигнут конец потока
        eof = -1;
      }
    }

    // Отправка файла
    void type(){
      head = f.OK+head+f.CL+": ";
      fs = File.OpenRead(res);
      nf = fs.Length;
      head += nf+"\r\n\r\n";
      i = UTF.GetBytes(head, 0, head.Length, buf, n1);
      i2 = fs.Read(buf, i, nbuf-i);            // Заполнить первую половину буфера синхронно
      t = stream.WriteAsync(buf, n1, i2+i);    // Асинхронно записать в поток
      k = n2;
      while (i2<nf) {
        i = fs.Read(buf, k, nbuf);             // Синхронно прочитать
        if(i>0) {
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
      if(filename.Length>0 || Content_Length>f.post){
        dirname=f.DirectorySessions+"/"+IP+"_"+point.Port.ToString();
        if(filename.Length==0) filename=DateTime.Now.ToString("HHmmssfff");
        filename = dirname+"/"+HttpUtility.UrlDecode(filename);
        return true;
      }
      return false;
    }

    // Передаем блок заголовков
    void res_start(){
      reso = res+"\nSCRIPT_FILENAME:"+f.fullres(ref res)+"\nQUERY_STRING:"+
             QUERY_STRING+"\nREMOTE_ADDR:"+IP;
      while (heads.Count>1) reso += "\n"+heads.Dequeue()+":"+heads.Dequeue();
      f.proc[m].StandardInput.WriteLine(reso.Length.ToString()+"\n"+reso);
    }

    // Передача данных из потока в объект
    void send_stream(byte b) {
      if(len<Content_Length) {
        l = true;
        while (l) {

          // Читаем асинхронно, первый буфер был прочитан при чтении заголовков
          sReadAsync();

          if(i>0) {
            i += len;
            i2 += i;
            sWrite(b);  // Пишем синхронно
            l = i2<Content_Length;
            len = 0;
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
      send_stream(0);
      if(file1.CanRead) file1.Close();
    }

    void send_cgi() {
      m = -1;
      fullprg = f.fullres(ref res);

      // Извлечь свободный номер CGI
      if(f.cgia) {
        try{
          m = f.freeCGI.Pop();
          if(f.cgib[m]==f.b0) f.start_CGI(m);
          f.cgib[m] = f.b2;
        } catch(Exception) {
          m = f.db;
        }
      }
      if(m < 0) {

        // Вывести сообщение об отсутствии интерпретатора
        send_prg1("There is no \""+f.Proc+"\" on the server :(");
        return;

      } else if(m >= f.db) {

        // Вывести сообщение, что все доступные процессы интерпретатора заняты
        send_prg1("All "+f.db.ToString()+"\""+f.Proc+"\" processes are busy :(");
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

      if(eof==0) {      // Если нет разрыва связи

        // Вывод полученных данных cgi-скрипта
        reso = f.OK+head;

        // Помещаем заголовок в буфер с позиции n2
        k = UTF.GetBytes(reso, 0, reso.Length, buf, n2);

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
                UTF.GetBytes(reso,0,reso.Length,buf,k1);
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
        while (i1>0) {
          t = stream.WriteAsync(buf, k1, i1);  // Асинхронно записать в поток
          k1 = k1<n2? n2 : n1;                 // Следующее начало буфера
          i1 = f.proc[m].StandardOutput.BaseStream.Read(buf, k1, nbuf);
          t.Wait();
        }
      }
      f.clear_cgi(m);
    }

    // Вывод текстового сообщения длиной до 1 буфера
    void send_prg1(string s) {
      string z = f.OK+head+f.CT_T+"\r\n"+s;
      i = UTF.GetBytes(z,0,z.Length,buf,0);
      stream.Write(buf,0,i);
    }

    // Прочитать i1 символов начиная с i2 в buf начиная с k1
    void stdioRead() {
      i2 += i1;            // Превести позицию в STD_IO
      if(i2>Content_Length) i1 -= i2-Content_Length;

      // Файлы выводятся только с таким кодированием
      f.vfpw.GetBytes(f.vfp[m].Eval("STD_IO.Read("+i1+")"), 0, i1, buf, k1);
    }

    void send_prg() {
      m = -1;
      fullprg=f.fullres(ref res);
      prg=f.afterStr9(ref res,"/");

      // Извлечь свободный номер БД
      if(f.vfpa != null) {
        try{
          m = f.freeVFP.Pop();
          if(f.vfpb[m]==f.b0) f.start_VFP(m);
          f.vfpb[m] = f.b2;
        } catch(Exception) {
          m = f.db;
        }
      }

      if(m < 0) {
        // Вывести сообщение об отсутствии VFP в реестре
        send_prg1("MS VFP is missing in the Windows registry :(");
        return;

      } else if(m >= f.db) {
        // Вывести сообщение, что все процессы VFP заняты
        send_prg1("All "+f.db.ToString()+" VFP processes are busy :(");
        return;

      } else {

        try {
          f.vfp[m].SetVar("ERROR_MESS","");
        } catch(System.Runtime.InteropServices.COMException) {
          f.start_VFP(m);
        }
        f.vfp[m].DoCmd("on erro ERROR_MESS='ERROR: '+MESSAGE()+' IN: '+MESSAGE(1)");
        f.vfp[m].DoCmd("SET DEFA TO (\""+f.beforStr9(ref fullprg,"/")+"\")");
        f.vfp[m].SetVar("SERVER_PROTOCOL",f.Protocol);
        f.vfp[m].SetVar("QUERY_STRING",QUERY_STRING);
        f.vfp[m].SetVar("SCRIPT_FILENAME",fullprg);
        f.vfp[m].SetVar("REMOTE_ADDR",IP);
        while (heads.Count>1) f.vfp[m].SetVar("_"+heads.Dequeue().Replace("-","_")+
              "_",heads.Dequeue());
        if(filename2()) {     // Определяем и проверяем наличие имя файла для POST-данных
          f.vfp[m].SetVar("POST_FILENAME",f.Folder+filename);
          send_file();        // Записываем в файл
        } else {
          f.vfp[m].SetVar("POST_FILENAME",filename);
          send_stream(R);     // Записываем в STD_IO в VFP
        }
        if(eof < 0) {         // Если обнаружен разрыв связи
          f.clear_prg(m);
          return;
        }
      }

      // Вывод полученных данных prg-скрипта
      try{
        head = f.OK+head;
        if(R1==f.b0){
          var ret = f.vfp[m].Eval(f.beforStr9(ref prg,".prg")+"()");
        }else{      // Случай API
          var ret = f.vfp[m].Eval(prg+"()");
          if(ret.GetType().Name=="String")
             if(ret.Length>5) {
               i = f.valInt(ret.Substring(0,4));
               if(i>=100 && i<=599) head = f.H1+ret+"\r\n";
          }
        }
        Content_Length = f.vfp[m].Eval("STD_IO.LenStream()");
      }catch(Exception e){
        head=f.OK+head+f.CT_T+"\r\nError in VFP: "+e.Message;
        Content_Length = 0;
      }

      // Начальная позиция в STD_IO
      i2 = 0;

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
