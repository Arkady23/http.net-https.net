//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                     !!
//!!   http.net сервер на C#.     Автор: A.Б.Корниенко   !!
//!!   Головной блок              версия от 06.11.2025   !!
//!!                                                     !!
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

using http1;
using http2;
using System;
using System.IO;
using System.Net;
using System.Text;
using System.Drawing;
using System.Threading;
using System.Diagnostics;
using System.Windows.Forms;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Collections.Generic;

public class f : Form {
    public static Semaphore maxNumberAcceptedClients;
    public static Stack<int> freeClientsPool;  // Стек индексов свободных(доступных) клиентов
    public static Stack<int> freeCGI;          // Стек индексов свободных процессов CGI
    public static Stack<int> freeVFP;          // Стек индексов свободных процессов VFP
    ContextMenu menu = new ContextMenu();
    IContainer conta = new Container();
    MenuItem menuQ = new MenuItem();
    MenuItem menuF = new MenuItem();
    MenuItem menuS = new MenuItem();
    MenuItem menuR = new MenuItem();
    NotifyIcon nIcon;
    TextBox textBox1;
    string[] param;
    Thread tSer;
    Server ser;

    private const string hs="http.net server";
    public const string CL="Content-Length",CT="Content-Type",CD="Content-Disposition",
                 CC="Cache-Control: public, max-age=2300000\r\n", OK=H1+"200 OK\r\n",
                 H1="HTTP/1.1 ",UTF8="UTF-8",CLR="sys(2004)+'VFPclear.prg'",DI="index.html",
                 Protocol="http", logX="http.net.x.log", logY="http.net.y.log",
                 CT_T=CT+": text/plain\r\n", stopIconText= hs+" is stopped",
                 initCGI= "initcgi.",
           //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                 ver="version 3.6.0", verD="November 2025";   //!!
           //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public const  int i2=2, i9=2147483647;
    public const  byte b0=0, b1=1, b2=2, b10=10, b13=13;
    public static int i, k, port, post, st, qu, bu, bu0, bu1, bu2, bu3, bu4, bu8, db, log9,
                  st1, tw, iIP, iIP1, nClients, s9=1000, logi=0;
    public static string IP, IP1, DocumentRoot, Folder=Thread.GetDomain().BaseDirectory,
                  DirectoryIndex, Proc, Args, Ext, logZ="", DirectorySessions;
    public static Icon ico = Icon.ExtractAssociatedIcon(Folder+"http.net.exe");
    public static bool notExit=false, notQuit=true, cgia, VFP9, VFPclr;
    public static Encoding vfpw = Encoding.GetEncoding(1251); // подходит для двоичных данных
    public static StreamWriter logSW = null;
    public static Session[] session = null;
    public static FileStream logFS = null;
    public static ProcessStartInfo[] cgi;
    public static dynamic[] vfp = null;
    public static byte[] vfpb, cgib;
    public static Type vfpa = null;
    public static Process[] proc;

    protected override void Dispose( bool disposing ) {
      // Clean up any container being used.
      if( disposing )
          if (conta != null) conta.Dispose();            
      base.Dispose( disposing );
    }

    void nIcon_DoubleClick(object Sender, EventArgs e) {
      // Set the WindowState to normal if the form is minimized.
      if(this.WindowState == FormWindowState.Minimized) {
         this.Show();
         this.WindowState = FormWindowState.Normal;
      }
      this.CenterToScreen();
      this.Activate();
    }
    void menuS_Click(object Sender, EventArgs e) {
      if(nIcon.Text==stopIconText) RunServer(param);
    }
    void menuF_Click(object Sender, EventArgs e) {
      StopServer();
    }
    void menuR_Click(object Sender, EventArgs e) {
      StopServer();
      RunServer(param);
    }
    void menuQ_Click(object Sender, EventArgs e) {
      notQuit = false;
      StopServer();
    }
    void StopIcon() {
      // Отобразить значок выключения
      if(notQuit) {
        nIcon.Icon = SystemIcons.Exclamation;
        nIcon.Text = stopIconText;
      }
    }

    [STAThread]
    static void Main (string[] args) {

      Directory.SetCurrentDirectory(Folder);
      if(!(ico != null)) ico = SystemIcons.Shield;

      // https://learn.microsoft.com/en-us/dotnet/api/system.windows.forms.notifyicon?view=windowsdesktop-9.0&redirectedfrom=MSDN
      Application.Run( new f(args));

    }

    public f(string[] args) {
      this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
      this.WindowState = FormWindowState.Minimized;
      this.FormClosing += Form_Close;
      this.ShowInTaskbar = false;
      this.Shown += Form_Shown;

      // Initialize menu
      this.menu.MenuItems.AddRange( new MenuItem[] {this.menuR,this.menuS,this.menuF,this.menuQ});

      this.menuR.Index = 0;
      this.menuR.Text = "R&eload";
      this.menuR.Click += new EventHandler(this.menuR_Click);

      this.menuS.Index = 1;
      this.menuS.Text = "S&tart";
      this.menuS.Click += new EventHandler(this.menuS_Click);

      this.menuF.Index = 2;
      this.menuF.Text = "F&inalize";
      this.menuF.Click += new EventHandler(this.menuF_Click);

      this.menuQ.Index = 3;
      this.menuQ.Text = "Q&uit";
      this.menuQ.Click += new EventHandler(this.menuQ_Click);

      // Set up how the form should be displayed.
      this.ClientSize = new Size(900,650);
      this.Text = hs;

      // Create the NotifyIcon.
      this.nIcon = new NotifyIcon(this.conta);

      // The Icon property sets the icon that will appear
      // in the systray for this application.
      nIcon.Icon = ico;

      // The ContextMenu property sets the menu that will
      // appear when the systray icon is right clicked.
      nIcon.ContextMenu = this.menu;

      // The Text property sets the text that will be displayed,
      // in a tooltip, when the mouse hovers over the systray icon.
      nIcon.Text = hs+" is starting...";
      nIcon.Visible = true;

      // Handle the DoubleClick event to activate the form.
      nIcon.DoubleClick += new EventHandler(this.nIcon_DoubleClick);

      AppDomain.CurrentDomain.ProcessExit += (sender, eventArgs) =>
      {
          StopServer();
      };

      // Анонимная функция перехвата и вывода ошибки
      AppDomain.CurrentDomain.UnhandledException += (sender, eventArgs) =>
      {
        log2(" "+((Exception)eventArgs.ExceptionObject).ToString());
        StopServer();
      };

      param = (string[])args.Clone();
      RunServer(args);
    }

    void Form_Shown(object sender, EventArgs e) {
      if(this.WindowState == FormWindowState.Minimized) {
        this.Hide();
      } else {
        this.Show();
      }
    }

    void Form_Close(object sender, CancelEventArgs e) {
      if (notQuit && session != null) {
        e.Cancel = true;  // кнопка больше не закрывает форму
        this.WindowState = FormWindowState.Minimized;
        this.Hide();
      }
    }

    private void RunServer(string[] args){

      // Установить значения сервера по умолчанию
      DirectorySessions="Sessions";
      DocumentRoot="../www/";
      Proc="python.exe";
      DirectoryIndex=DI;
      post=33554432;
      iIP=iIP1=0;
      IP=IP1="-";
      log9=10000;
      port=8080;
      bu=131072;
      Ext="pyc";
      tw=10000;
      Args="";
      qu=100;
      st=300;
      db=30;

      if(getArgs(args)){
        if(Args.Length>0) Args+=" ";

        // Создать стек индексов клиентов с симофорным контролем максимального значения
        maxNumberAcceptedClients = new Semaphore(st-2,st-2);
        freeClientsPool = new Stack<int>(st);
        // Заполнить стек клиентов индексами
        for (i=0; i<st; i++) freeClientsPool.Push(i);

        // Разделить буфер для ускорения чтения
        bu4 = bu/4;
        bu1 = bu-3*bu4;
        bu2 = bu1+bu4;
        bu3 = bu2+bu4;
        bu8 = bu4+bu4;
        bu0 = bu - 1;

        st1 = st>133? 100 : st*3/4;   // Позволено запросов от одного IP
        nClients = st;                // Начальное число соединений

        // Создать объекты сессий предварительно очистив сессии от предыдущих запусков
        session = null;
        i = st<2? 4 : st;
        ThreadPool.SetMinThreads(i,i);
        session = new Session[st];
        try{
          Parallel.For(0,st,j => { session[j] = new Session(j); });
          notExit=true;
        }catch(Exception){
          if(log9>0) log("\tThere were problems when creating threads. Try updating Windows.");
        }

        if(notExit) {

          // Запустить экземпляр CGI
          proc = new Process[db];
          cgi = new ProcessStartInfo[db];
          cgia = start_CGI(0);
          if(cgia) {

            // Свободные номера просессов для CGI
            freeCGI = new Stack<int>(db);
            for (i=db; i>0; ) freeCGI.Push(--i);

            cgib = new byte[db];
            cgib[0] = b1;
          } else {
            log("\tThe \""+Proc+("\" interpreter or\r\n".PadRight(41))+
                "\tthe \""+DocumentRoot+initCGI+Ext+"\" script could not be run.");
          }

          // Запустить и настроить жкземпляр VFP
          VFPclr = false;
          vfpa = Type.GetTypeFromProgID("VisualFoxPro.Application");
          if(vfpa!=null){
            vfp = new dynamic[db];
            vfpb = new byte[db];
            try{
              vfp[0] = Activator.CreateInstance(vfpa);
            }catch(Exception){
              vfpa = null;
            }
            if(vfpa!=null){
              VFP9= vfp[0].Eval("sys(17)")=="Pentium";
              try {
                start_VFP2(0);
              } catch(Exception) {
                log("\tCOM server 'VFP.memlib"+(VFP9?"32'":"'")+" is not registered in Windows registry.");
                vfpa = null;
              }
            }
            if(vfpa!=null){
              vfpb[0] = b1;

              // Не уверен, что это будет правильно. Пока придерживаюсь версии,
              // что только Windows-1251 может кодировать двоичные данные.
              // Поэтому вывел эту кодировку на глобальный уровень доступности.
              //vfpw=Encoding.GetEncoding(vfp[0].Eval("CPCURRENT()"));
              VFPclr=vfp[0].Eval("file("+CLR+")");

              // Свободные номера баз данных
              freeVFP = new Stack<int>(db);
              for (i=db; i>0; ) freeVFP.Push(--i);
            }
          }

          // Запускаем движок http
          if(Directory.Exists(DirectorySessions)) Directory.Delete(DirectorySessions,true);
          IPEndPoint ep = new IPEndPoint(IPAddress.Any, port);
          ser = new Server();
          if(ser.Start(ep)) {

            // Запуск чтения сокета
            tSer = new Thread(ser.StartAccept);
            tSer.Start();

            // Отобразить значок работы
            nIcon.Icon = ico;  // SystemIcons.Shield;
            nIcon.Text = hs+" is running";
            if(log9>0) log("\tThe "+hs+" "+ver+" is running.");

          } else {
            notExit = false;   // Отметить для возможности снятия, т.к. сервер запущен

            // Отобразить значок выключения
            this.StopIcon();
          }
        } else {

          // Отобразить значок выключения
          this.StopIcon();
        }

      }else{

        // Неверные параметры запуска, закрыть приложение
        nIcon.Text = hs;
        this.Show();
        this.WindowState = FormWindowState.Normal;
        this.CenterToScreen();
        this.Activate();
      }
    }

    public void StopServer(){
      if(notExit){
        notExit = false;

        // Остановить движок
        ser.Stop();
        ser = null;

        // Закрыто все процессы интерпретатора
        if(cgia) for(i=0; i<db; i++) if(cgib[i]>b0)
                 try{ f.proc[i].StandardInput.WriteLine(""); }
                 catch(Exception) { }
        proc = null;
        cgib = null;
        cgi = null;

        // Закрыто все процессы VFP
        if(vfpa != null) for(i=0; i<db; i++) if(vfpb[i]>b0)
                try{ vfp[i].Quit(); }catch(Exception){ }
        vfpb = null;
        vfpa = null;
        vfp = null;
    
        // Отобразить значок выключения
        this.StopIcon();

        if(log9>0) log("\tThe "+hs+" is stopped.");
      }
      if(!notQuit) this.Close();
    }

    public static string ltri(ref string x){
      return x.TrimStart('\t',' ');
    }

    public static string fullres(ref string x){
      return Path.GetFullPath(x).Replace("\\","/");
    }

    public static string beforStr1(ref string x, string Str){
      int k=0;
      if(Str.Length>0) k=x.IndexOf(Str);
      return k<0?x:(k>0?x.Substring(0,k):"");
    }

    public static string afterStr1(ref string x, string Str){
      if(Str.Length>0){
        int k=x.IndexOf(Str,StringComparison.OrdinalIgnoreCase);
        return k<0?"":x.Substring(k+Str.Length);
      }else{
        return x;
      }
    }

    public static string beforStr9(ref string x, string Str){
      if(Str.Length>0){
         int k=x.LastIndexOf(Str);
         return k<0?x:(k>0?x.Substring(0,k):"");
      }else{
         return x;
      }
    }

    public static string afterStr9(ref string x, string Str){
      int k= -1;
      if(Str.Length>0) k=x.LastIndexOf(Str);
      return k<0?"":x.Substring(k+Str.Length);
    }

    // Узнать значение поля в заголовке (может понадобиться при разборе заголовков)
    public static string valStr(ref string x, string Str){
      string z="";
      if(x.Length>0){
        z=afterStr1(ref x," "+Str+"=");
        if(z.Length==0) z=afterStr1(ref x,";"+Str+"=");
        if(z.Length>0){
          if(z.Substring(0,1)=="\""){
            z=z.Substring(1);
            z=beforStr1(ref z,"\"");
          }else{
            z=beforStr1(ref z,";");
          }
        }
      }
      return z;
    }

    public static void log(object x){
      // Добавить сообщение в журнал с чередующимися версиями.
      // Сначала писать в X, затем в Y, затем снова в X и т.д.

      // Нужно ли начать запись в другой журнал?
      if(logi>=log9 && logFS!=null){
        Interlocked.Exchange(ref logi,1);
        logZ = (logY==logZ)? logX:logY;
        logSW.Close();
        logFS.Close();
        log1();
      }else{
        Interlocked.Increment(ref logi);
      }

      if(!(logFS!=null)){
        // Отправка стандартного вывода на консоль в чередующиеся кешируемые файлы:
        logZ=(File.GetLastWriteTime(logX)<=File.GetLastWriteTime(logY))? logX : logY;
        log1();
      }

      // Записать в файл
      try{
        Console.WriteLine(DateTime.Now.ToString("dd.MM.yyyy HH:mm:ss.fff")+x);
        logSW.Flush();
        logFS.Flush();
      }catch(ObjectDisposedException){
        log9 = 0;
      }catch(Exception){
        Thread.Sleep(23); log2(x+" *");
      }
    }

    internal static void log1(){
      logFS = new FileStream(logZ,FileMode.Create,FileAccess.Write,FileShare.ReadWrite);
      logSW = new StreamWriter(logFS);
      Console.SetError(logSW);
      Console.SetOut(logSW);
    }

    public static void log2(string x){
      if(log9>0){
        Thread log2 = new Thread(log);
        log2.Priority = ThreadPriority.BelowNormal;
        log2.Start(x);
      }
    }

    public static int valInt(string x){
      int z;
      try { z=int.Parse(x); } catch(Exception) { z=i9; }
      return z;
    }

    // Запуск скрипта initCGI
    public static bool start_CGI(int i) {

      // Проверим работает ли этот процесс
      try {
        if( !proc[i].HasExited) {
           return true;
        }
      } catch(Exception) { }

      // Если процесс не работает, то запустим
      cgi[i] = new ProcessStartInfo();
      cgi[i].FileName = Proc;
      cgi[i].CreateNoWindow = true;
      cgi[i].UseShellExecute = false;
      cgi[i].RedirectStandardInput = true;
      cgi[i].RedirectStandardOutput = true;
      cgi[i].EnvironmentVariables["SERVER_PROTOCOL"] = Protocol;
      cgi[i].Arguments = Args+" \""+DocumentRoot+initCGI+Ext+"\"";
      try {
        proc[i] = Process.Start(cgi[i]);
      } catch(Exception) {
        return false;
      }
      return true;
    }

    // Подготовим CGI к новым заданиям
    public static void clear_cgi(int m) {
      cgib[m] = start_CGI(m)? b1: b0;
      freeCGI.Push(m);
    }

    // Запуск VFP
    public static void start_VFP(int m) {
      vfp[m] = Activator.CreateInstance(vfpa);
      start_VFP2(m);
    }
    public static void start_VFP2(int m) {
      vfp[m].DoCmd("STD_IO=CreateO('VFP.memlib"+(VFP9?"32')":"')"));
    }

    // Подготовим VFP к новым заданиям
    public static void clear_prg(int m) {
      try{
        if(VFPclr){
          vfp[m].DoCmd("do ("+CLR+")");
          start_VFP2(m);
        }else{
          vfp[m].DoCmd("STD_IO.CloseAll()");
          vfp[m].DoCmd("clos data all");
          vfp[m].DoCmd("clea even");
          vfp[m].DoCmd("clea prog");
          vfp[m].DoCmd("clea all");
          vfp[m].DoCmd("clos all");
          start_VFP2(m);
        }
        vfpb[m] = b1;
      }catch(Exception){
        vfpb[m] = b0;
      }
      if(vfpb[m]==b0) {
        try{ vfp[m].Quit(); }catch(Exception){ }
        start_VFP(m);
        vfpb[m] = b1;
      }
      freeVFP.Push(m);
    }

    // Освободить индекс клиента и сделать его доступным
    public static void freeSession(int j) {
      freeClientsPool.Push(j);
      maxNumberAcceptedClients.Release();
    }

    int odd(string z) {
      return (z.Length - z.Replace("'", "").Length)%2 +
             (z.Length - z.Replace("\"", "").Length)%2;
    }

    private bool getArgs(String[] args){
      const int b9=131072, db9=1000, p9=65535, q9=2147483647, post9=33554432, b0=512,
                log0=80, t9=20;
      int i, k;
      bool l=true;
      if(args.Length>0){
        if((byte)args[0][0]==64){
          string fn=args[0].Substring(1).Trim();
          if(File.Exists(fn))
             fn = File.ReadAllText(fn).Replace("\t", " ").Replace("\r",string.Empty).
                    Replace("\n",string.Empty).Trim();
          k = 1;
          while (k<fn.Length) {
            i = fn.IndexOf(" ",k);
            if(i<0) {
              k = fn.Length;
            } else {
              if(odd(fn.Substring(k, i-k-1))==0) 
                 fn = fn.Substring(0,i)+"\t"+fn.Substring(i+1);
              k = i+1;
            }
          }
          args = fn.Split('\t');
          for (i = 0; i<args.Length; i++) {
            if (args[i].Length>1) {
              if (args[i][0]==args[i][args[i].Length-1]) {
                if (args[i][0]=='"' || args[i][0]=='\'')
                    args[i] = args[i].Substring(1,args[i].Length-2);
              }
            }
          }
        }
      }

      // Разбор параметров
      for (i = 0; i < args.Length; i++){
        switch (args[i]){
        case "-p":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            if(k > 0 && k <= p9) port=k;
          }
          break;
        case "-b":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            if(k<b0){
              bu=b0;
            }else{
              bu=(k <= b9)? k : b9;
            }
          }            
          break;
        case "-q":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            qu=(k > 0 && k <= q9)? k : q9;
          }            
          break;
        case "-s":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            st= k>3? (k<=s9? k : s9) : 4;
          }            
          break;
        case "-n":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            if(k >= 0 && k <= db9) db=k;
          }            
          break;
        case "-w":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            tw=((k > 0 && k <= t9)? k : t9)*1000;
          }            
          break;
        case "-log":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            log9=(k < log0)? 0 : k;
          }            
          break;
        case "-post":
          i++;
          if(i < args.Length){
            k=valInt(args[i]);
            post=(k > 0)? k : post9;
          }            
          break;
        case "-d":
          i++;
          if(i < args.Length) DocumentRoot=
            (args[i].EndsWith("/")||args[i].EndsWith("\\"))?args[i]:args[i]+"/";
          break;
        case "-i":
          i++;
          if(i < args.Length) DirectoryIndex=args[i];
          break;
        case "-proc":
          i++;
          if(i < args.Length) Proc=args[i];
          break;
        case "-args":
          i++;
          if(i < args.Length) Args=args[i];
          break;
        case "-ext":
          i++;
          if(i < args.Length) Ext=args[i];
          break;
        default:
          l=false;
          break;
        }
        if(!l) break;
      }
      textBox1 = new TextBox()
      {
        Location = new Point(5,5),
        Size = new Size(this.ClientSize.Width-10,this.ClientSize.Height-10)
      };
      textBox1.TabStop = false;
      textBox1.ReadOnly = true;
      textBox1.Multiline = true;
      textBox1.ScrollBars = ScrollBars.Vertical;
      textBox1.Font = new Font("Consolas", 13);
      textBox1.WordWrap = true;
      textBox1.Text = "Multithreaded "+hs+" "+ver+", (C) a.kornienko.ru "+verD+@".

USAGE:
    http.net [Parameter1 Value1] [Parameter2 Value2] ...
    http.net @filename

    If necessary, Parameter and Value pairs are specified. If the value is text and contains
    spaces, then it must be enclosed in quotation marks. You can specify @filename which
    contains the entire line with parameters.

Parameters:                                                                  Values:
     -d      Folder containing the domains.                                      "+DocumentRoot+@"
     -i      Main document is in the folder. The main document in the            "+DirectoryIndex+@"
             folder specified by the -d parameter is used to display the page
             with the 404 code - file was not found. To compress traffic,
             files compressed using gzip method of the name.expansion.gz type
             are supported, for example - index.html.gz or library.js.gz etc.
     -p      Port that the server is listening on.                               "+port.ToString()+@"
     -b      Size of read/write buffers.                                         "+bu.ToString()+@"
     -s      Number of requests being processed at the same time. Maximum        "+st.ToString()+@"
             value is "+s9.ToString()+@".
     -q      Number requests stored in the queue.                                "+qu.ToString()+@"
     -w      Allowed time to reserve an open channel for request that did not    "+(tw/1000).ToString()+@"
             started. From 1 to "+t9.ToString()+@" seconds.
     -n      Maximum number of dynamically running interpreters or MS VFP        "+db.ToString()+@"
             instances. Processes are launched as needed depending on the
             number of concurrent requests. Maximum value is "+db9.ToString()+@".
     -log    Size of the query log in rows. The log consists of two              "+log9.ToString()+@"
             interleaved versions http.net.x.log and http.net.y.log. If the
             size is set to less than "+log0.ToString()+@", then the log is not kept.
     -post   Maximum size of the accepted request to transfer to the script      "+post.ToString()+@"
             file. If it is exceeded, the request is placed in a file,
             the name of which is passed to the script in the environment
             variable POST_FILENAME. Other generated environment variables -
             SERVER_PROTOCOL, SCRIPT_FILENAME, QUERY_STRING, HTTP_HEADERS,
             REMOTE_ADDR. If the form-... directive is missing from the
             request data, then incoming data stream will be placed entirely
             in a file. This feature can be used to transfer files to the
             server. In this case, the file name will be in the environment
             variable POST_FILENAME.
     -proc   Script handler used. If necessary, you must also include            "+Proc+@"
             the full path to the executable file.
     -args   Additional parameters of the handler startup command line.
     -ext    Extension of the script files.                                      "+Ext;

      Controls.Add(textBox1);

      return l;
    }
}
