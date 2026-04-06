//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
//!!                                                     !!
//!!   https.net сервер на C#.    Автор: A.Б.Корниенко   !!
//!!   Головной блок              версия от 06.04.2026   !!
//!!                                                     !!
//!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!

using https1;
using https2;
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
using System.Security.Cryptography.X509Certificates;

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

    private const string hn="https.net";
    private const string hs=hn+" server";
    public const string CL="Content-Length",CT="Content-Type",CD="Content-Disposition",
                 CC="Cache-Control: public, max-age=2300000\r\n", OK=H1+"200 OK\r\n",
                 H1="HTTP/1.1 ",UTF8="UTF-8",CLR="sys(2004)+'VFPclear.prg'",DI="index.html",
                 Protocol="https", logX=hn+".x.log", logY=hn+".y.log",
                 CT_T=CT+": text/plain\r\n", stopIconText= hs+" is stopped",
                 initCGI= "initcgi.",
           //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
                 ver="version 1.9.0", verD="April 2026";      //!!
           //!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
    public const  int i0=0, i1=1, i2=2, i8=1500000, i9=2147483647;
    public const  byte b0=0, b1=1, b2=2, b3=3, b10=10, b13=13;
    public static int i, k, port, post, st, qu, bu, bu0, bu1, bu2, bu3, bu4, bu8, db,
                  log9, st1, qu1, tw, iIP, iIP1, nClients, s9=1000, logi=i0;
    public static string IP, IP1, DocumentRoot, Folder=Thread.GetDomain().BaseDirectory,
                  DirectoryIndex, Proc, Args, Ext, logZ=string.Empty, DirectorySessions;
    private static string Fullexe = Folder+hn+".exe";
    public static bool notExit=false, notQuit=true, cgia, VFP9, VFPclr;
    public static Icon ico = Icon.ExtractAssociatedIcon(Fullexe);
    public static Encoding vfpw = Encoding.GetEncoding(1251); // подходит для двоичных данных
    public static X509Certificate2 cert = null;
    public static StreamWriter logSW = null;
    public static Session[] session = null;
    public static FileStream logFS = null;
    public static ProcessStartInfo[] cgi;
    public static dynamic[] vfp = null;
    public static byte[] vfpb, cgib;
    public static Type vfpa = null;
    public static Process[] proc;
    public static int[] vfpi;
    string CerFile;

    protected override void Dispose( bool disposing ) {
      // Clean up any container being used.
      if( disposing )
          if (conta != null) conta.Dispose();            
      base.Dispose( disposing );
    }

    void nIcon_BalloonTipClosed(object Sender, EventArgs e) {

      // Отображались ошибки в параметрах запуска
      this.Close();
    }

    void nIcon_BalloonTipClicked(object Sender, EventArgs e) {

      // Отображались ошибки в параметрах запуска
      this.Close();
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
      this.WindowState = FormWindowState.Minimized;
      this.FormBorderStyle = FormBorderStyle.FixedToolWindow;
      this.FormClosing += Form_Close;
      this.ShowInTaskbar = false;
      this.Shown += Form_Shown;

      // Initialize menu
      this.menu.MenuItems.AddRange( new MenuItem[] {this.menuR,this.menuS,this.menuF,this.menuQ});

      this.menuR.Index = i0;
      this.menuR.Text = "R&eload";
      this.menuR.Click += new EventHandler(this.menuR_Click);

      this.menuS.Index = i1;
      this.menuS.Text = "S&tart";
      this.menuS.Click += new EventHandler(this.menuS_Click);

      this.menuF.Index = i2;
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

      // Событие закрытия уведомления
      nIcon.BalloonTipClosed += new EventHandler(nIcon_BalloonTipClosed);
      nIcon.BalloonTipClicked += new EventHandler(nIcon_BalloonTipClicked);

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

    private async void RunServer(string[] args){

      // Установить значения сервера по умолчанию
      DirectorySessions="Sessions";
      CerFile="kornienko.ru.pfx";
      DocumentRoot="../www/";
      Proc="python.exe";
      DirectoryIndex=DI;
      Args=string.Empty;
      post=33554432;
      iIP=iIP1=i0;
      IP=IP1="-";
      log9=10000;
      port=8443;
      bu=131072;
      Ext="pyc";
      tw=10000;
      qu=100;
      st=100;
      st1=16;
      qu1=8;
      db=30;

      if(getArgs(args)){
        if(notQuit) {
          if(Args.Length>i0) Args+=" ";

          // Создать объект cert
          if(!File.Exists(CerFile)) {
            CerFile=DocumentRoot+CerFile;
            if(!File.Exists(CerFile)) CerFile=string.Empty;
          }
          if(CerFile==string.Empty) {
            log2("\tThe "+hs+" cannot work. Certificate was not found :(");
            notExit=true;
          } else {
            try {
              cert = new X509Certificate2(CerFile);
            } catch(Exception) {
              log2("The "+hs+" cannot work. Certificate error :((");
              cert = null;
            }
            if(cert!=null) notExit=true;
          }

          // Создать стек индексов клиентов с симофорным контролем максимального значения
          maxNumberAcceptedClients = new Semaphore(st-i2,st-i2);
          freeClientsPool = new Stack<int>(st);
          // Заполнить стек клиентов индексами
          for (i=i0; i<st; i++) freeClientsPool.Push(i);

          // Разделить буфер для ускорения чтения
          bu4 = bu/4;
          bu1 = bu-3*bu4;
          bu2 = bu1+bu4;
          bu3 = bu2+bu4;
          bu8 = bu4+bu4;
          bu0 = bu - i1;

          // Общая длина очереди
          try { qu *= st; } catch(Exception) { qu = i9; };

          // Создать объекты сессий предварительно очистив сессии от предыдущих запусков
          nClients = st;                    // Начальное число соединений
          ThreadPool.SetMinThreads(st,st);
          session = new Session[st];
          try{
            Parallel.For(i0,st,j => { session[j] = new Session(j); });
            notExit=true;
          }catch(Exception){
            if(log9>i0) log("\tThere were problems when creating threads. Try updating Windows.");
          }
        }
        if(notExit) {

          // Запустить экземпляр CGI
          cgib = new byte[db];
          proc = new Process[db];
          cgi = new ProcessStartInfo[db];
          cgia = ! await start_CGI(i0);
          if(cgia) {

            // Свободные номера просессов для CGI
            freeCGI = new Stack<int>(db);
            for (i=db; i>i0; ) freeCGI.Push(--i);

            cgib[i0] = b1;
          } else {
            log("\tThe \""+Proc+("\" interpreter or\r\n".PadRight(41))+
                "\tthe \""+DocumentRoot+initCGI+Ext+"\" script could not be run.");
          }

          // Запустить и настроить экземпляр VFP
          VFPclr = false;
          vfpa = Type.GetTypeFromProgID("VisualFoxPro.Application");
          if(vfpa!=null){
            vfp = new dynamic[db];
            vfpb = new byte[db];
            vfpi = new int[db];
            try{
              vfp[i0] = Activator.CreateInstance(vfpa);
              vfpb[i0]=b1;
            }catch(Exception){
              vfpa = null;
            }
            if(vfpa!=null){
              VFP9= vfp[i0].Eval("sys(17)")=="Pentium";
              if(start_VFP2(i0)) {
                log("\tCOM server 'VFP.memlib"+(VFP9?"32'":"'")+" is not registered in Windows registry.");
                vfpa= null;
              }
            }
            if(vfpa!=null){
              VFPclr= vfp[i0].Eval("file("+CLR+")");
              vfpi[i0]= vfp[i0].ProcessID;
              start_VFP3(i0);

              // Свободные номера баз данных
              freeVFP= new Stack<int>(db);
              for (i=db; i>i0; ) freeVFP.Push(--i);
            }
          }

          // Запускаем движок https
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
            if(log9>i0) log("\tThe "+hs+" "+ver+" is running.");

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

        // Отобразить значок выключения
        this.StopIcon();

        // Закрыть все процессы интерпретатора
        if(cgia) for(i=i0; i<db; i++) if(cgib[i]>b0)
                 try{ f.proc[i].StandardInput.WriteLine(string.Empty); }
                 catch(Exception) { }
        proc = null;
        cgib = null;
        cgi = null;

        // Закрыто все процессы VFP
        if(vfpa != null) for(i=i0; i<db; i++) if(vfpb[i]>i0)
                try{ vfp[i].Quit(); }catch(Exception){ }
        vfpb = null;
        vfpa = null;
        vfpi = null;
        vfp = null;
    
        if(log9>i0) log("\tThe "+hs+" is stopped.");
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
      int k=i0;
      if(Str.Length>i0) k=x.IndexOf(Str);
      return k<i0?x:(k>i0?x.Substring(i0,k):string.Empty);
    }

    public static string afterStr1(ref string x, string Str){
      if(Str.Length>i0){
        int k=x.IndexOf(Str,StringComparison.OrdinalIgnoreCase);
        return k<i0?string.Empty:x.Substring(k+Str.Length);
      }else{
        return x;
      }
    }

    public static string beforStr9(ref string x, string Str){
      if(Str.Length>i0){
         int k=x.LastIndexOf(Str);
         return k<i0?x:(k>i0?x.Substring(i0,k):string.Empty);
      }else{
         return x;
      }
    }

    public static string afterStr9(ref string x, string Str){
      int k= -i1;
      if(Str.Length>i0) k=x.LastIndexOf(Str);
      return k<i0?string.Empty:x.Substring(k+Str.Length);
    }

    // Узнать значение поля в заголовке (может понадобиться при разборе заголовков)
    public static string valStr(ref string x, string Str){
      string z=string.Empty;
      if(x.Length>i0){
        z=afterStr1(ref x," "+Str+"=");
        if(z.Length==i0) z=afterStr1(ref x,";"+Str+"=");
        if(z.Length>i0){
          if(z.Substring(i0,i1)=="\""){
            z=z.Substring(i1);
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
        Interlocked.Exchange(ref logi,i1);
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
        log9=i0;
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
      if(log9>i0){
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
    public static async Task<bool> start_CGI(int i) {
      bool l;

      // Чтобы была асинхронность
      Task t = Task.Run(() => { cgib[i] = b2; });

      // Проверим работает ли этот процесс
      try {
        l = proc[i].HasExited;
      } catch(Exception) {
        l = true;
      }

      if( l ) {

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
          l = false;
        } catch(Exception) { }
      }
      await t;
      return l;
    }

    // Подготовим CGI к новым заданиям
    public static async Task clear_cgi(int m) {
      cgib[m] = await start_CGI(m)? b0: b1;
      freeCGI.Push(m);
    }

    // Запуск VFP
    public static async Task<bool> start_VFP(int m) {

      // Чтобы была асинхронность
      Task t = Task.Run(() => { vfpb[m] = b2; });

      bool l = start_VFP2(m);
      await t;
      return l;
    }

    public static bool start_VFP2(int m) {
      if(vfpb[m]!=b0) {
        try {
          start_VFP3(m);
          return false;
        } catch(Exception) { }
      }

      try {
        vfp[m]= Activator.CreateInstance(vfpa);
        vfpi[m]= vfp[m].ProcessID;
        start_VFP3(m);
        return false;
      } catch(Exception) { }
      return true;
    }

    public static void start_VFP3(int m) {
      vfp[m].DoCmd("on erro ERROR_MESS='ERROR: '+MESSAGE()+' IN: '+MESSAGE(1)");
      vfp[m].DoCmd("STD_IO=CreateO('VFP.memlib"+(VFP9?"32')":"')"));
      vfp[m].SetVar("SERVER_PROTOCOL",Protocol);
      vfp[m].SetVar("ERROR_MESS",string.Empty);
    }

    // Подготовим VFP к новым заданиям
    public static async void clear_prg(int m) {
      try{
        if(VFPclr){
          vfp[m].DoCmd("do ("+CLR+")");
        }else{
          vfp[m].DoCmd("STD_IO.CloseAll()");
          vfp[m].DoCmd("clos data all");
          vfp[m].DoCmd("clea even");
          vfp[m].DoCmd("clea prog");
          vfp[m].DoCmd("clea all");
          vfp[m].DoCmd("clos all");
        }
        start_VFP2(m);
        vfpb[m]=b1;
      }catch(Exception){
        vfpb[m]=b0;
      }
      if(vfpb[m]==b0) {
        try { vfp[m].Quit(); } catch(Exception) { }
        await start_VFP(m);
        vfpb[m]=b1;
      }
      freeVFP.Push(m);
    }

    // Освободить индекс клиента и сделать его доступным
    public static void freeSession(int j) {
      freeClientsPool.Push(j);
      maxNumberAcceptedClients.Release();
    }

    // Аварийно снимаем COM-процесс
    public static void killVFP(int m) {
      try { Process.GetProcessById(vfpi[m]).Kill(); }
      catch(Exception) { }
    }

    // Выполнить команду "schtasks"
    private bool schtasks(ref string par){
      bool ret;
      string output;
      byte[] buf = new byte[100];
      var ps = new ProcessStartInfo();
      ps.FileName = "schtasks";
      ps.CreateNoWindow = true;
      ps.UseShellExecute = false;
      ps.RedirectStandardOutput = true;
      ps.Arguments = par;
      try {
        Process p = Process.Start(ps);
        output = Encoding.GetEncoding(866).GetString(buf,i0,
                 p.StandardOutput.BaseStream.Read(buf,i0,100));
        p.WaitForExit();
        ret = true;
      } catch(Exception) {
        output = "FAILED :-(";
        ret = false;
      }
      if(output.Length>i2) {
        nIcon.ShowBalloonTip(6100, "Schtasks command", output,
              ret? ToolTipIcon.Info:ToolTipIcon.Error);
      }
      return ret;
    }

    int odd(string z) {
      return (z.Length - z.Replace("'", string.Empty).Length)%i2 +
             (z.Length - z.Replace("\"", string.Empty).Length)%i2;
    }

    string toStd(string z) {
      return z.Contains(" ")? "\""+z+"\"": z;
    }

    bool toArg(string[] args) {
      i++;
      return i<args.Length;
    }

    private bool getArgs(String[] args){
      const int b9=131072, db9=1000, p9=65535, post9=33554432, b0=512, log0=80, t9=20;
      string tx=string.Empty, ts=string.Empty, cA="Arguments>", fn=hn+".xml";
      bool l=true;
      int k1;

      // Если введён ключ вида /? или -? или /help или -help
      if (args.Length==i1) l = args[i0].Length>9;

      if(File.Exists(fn)) {
        if(args.Length==i0 || !l) {
          tx = File.ReadAllText(fn);
          k = tx.IndexOf("<"+cA,StringComparison.OrdinalIgnoreCase)+11;
          tx = tx.Substring(k, tx.IndexOf("</"+cA,StringComparison.OrdinalIgnoreCase)-k).
               Replace("\t", " ").Replace("\r"," ").Replace("\n"," ").Trim();
          k1 = k = i0;
          while (k<tx.Length) {
            i = tx.IndexOf(" ", k);
            if(i<i0) {
              k = tx.Length;
            } else {
              if(odd(tx.Substring(k1, i-k1))==i0) {
                if(i>k) {
                  tx = tx.Substring(i0,i)+"\t"+tx.Substring(i+i1);
                } else {
                  tx = tx.Substring(i0,i)+tx.Substring(i+i1);
                  i--;
                }
                k1 = i+i1;
              }
              k = i+i1;
            }
          }
          args = tx.Split('\t');
          for (i = i0; i<args.Length; i++) {
            if (args[i].Length>i1) {
              if (args[i][i0]==args[i][args[i].Length-i1]) {
                if (args[i][i0]=='"' || args[i][i0]=='\'')
                    args[i] = args[i].Substring(i1,args[i].Length-i2);
              }
            }
          }
        }
        tx = string.Empty;
      } else if(args.Length>i0) {
        tx = "<?xml version=\"1.0\" encoding=\"UTF-16\"?>"+@"
<Task version="+"\"1.2\" xmlns=\"http://schemas.microsoft.com/windows/2004/02/mit/task\">"+@"
  <Triggers>
    <BootTrigger>
      <Enabled>true</Enabled>
    </BootTrigger>
  </Triggers>
  <Principals>
    <Principal>
      <RunLevel>HighestAvailable</RunLevel>
    </Principal>
  </Principals>
  <Settings>
    <ExecutionTimeLimit>PT0S</ExecutionTimeLimit>
  </Settings>
  <Actions>
    <Exec>
      <Command>"+toStd(Fullexe)+@"</Command>
      <Arguments></Arguments>
    </Exec>
  </Actions>
</Task>
";
      }

      // Разбор параметров
      for (i = i0; i < args.Length; i++){
        switch (args[i]){
        case "-p":
          if(toArg(args)){
            k=valInt(args[i]);
            if(k > i0 && k <= p9) port=k;
          }
          break;
        case "-b":
          if(toArg(args)){
            k=valInt(args[i]);
            if(k<b0){
              bu=b0;
            }else{
              bu=(k <= b9)? k : b9;
            }
          }            
          break;
        case "-q":
          if(toArg(args)){
            k=valInt(args[i]);
            qu=(k > i0)? k : i9;
          }            
          break;
        case "-q1":
          if(toArg(args)) {
            k=valInt(args[i]);
            qu1= k > i0? k : i1;
          }
          break;
        case "-s":
          if(toArg(args)){
            k=valInt(args[i]);
            st= k>3? (k<=s9? k : s9) : 4;
          }            
          break;
        case "-s1":
          if(toArg(args)) {
            k=valInt(args[i]);
            st1= k > i0? k : i1;
          }
          break;
        case "-n":
          if(toArg(args)){
            k=valInt(args[i]);
            if(k >= i0 && k <= db9) db=k;
          }            
          break;
        case "-w":
          if(toArg(args)){
            k=valInt(args[i]);
            tw=((k > i0 && k <= t9)? k : t9)*1000;
          }            
          break;
        case "-log":
          if(toArg(args)){
            k=valInt(args[i]);
            log9=(k < log0)? i0 : k;
          }            
          break;
        case "-post":
          if(toArg(args)){
            k=valInt(args[i]);
            post=(k > i0)? k : post9;
          }            
          break;
        case "-d":
          if(toArg(args)) DocumentRoot=
            (args[i].EndsWith("/")||args[i].EndsWith("\\"))?args[i]:args[i]+"/";
          break;
        case "-i":
          if(toArg(args)) DirectoryIndex=args[i];
          break;
        case "-c":
          if(toArg(args)) CerFile=args[i];
          break;
        case "-proc":
          if(toArg(args)) Proc=args[i];
          break;
        case "-args":
          if(toArg(args)) Args=args[i];
          break;
        case "-ext":
          if(toArg(args)) Ext=args[i];
          break;
        case "/regserver":
          ts = "/create /tn "+hn+" /ru system /xml "+fn;
          if(tx.Length>i0) File.WriteAllText(fn,tx);
          i = args.Length;
          notQuit = false;
          break;
        case "/unregserver":
          ts = "/delete /f /tn \\"+hn;
          i = args.Length;
          notQuit = false;
          break;
        default:
          l=false;
          break;
        }
      }

      if(ts.Length>i0) schtasks(ref ts);

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
    https.net [Parameter1 Value1] [Parameter2 Value2] ...
    https.net /regserver              Starting the server when the computer is turned on.
    https.net /unregserver            Deleting the server startup task.

    If necessary, Parameter and Value pairs are specified. If the value is text and contains
    spaces, then it must be enclosed in quotation marks. You can also specify the parameters
    string in the "+fn+@" file in the <Arguments></Arguments> section.

Parameters:                                                                  Values:
     -d      Folder containing the domains.                                      "+DocumentRoot+@"
     -i      Main document is in the folder. The main document in the            "+DirectoryIndex+@"
             folder specified by the -d parameter is used to display the page
             with the 404 code - file was not found. To compress traffic,
             files compressed using gzip method of the name.expansion.gz type
             are supported, for example - index.html.gz or library.js.gz etc.
     -c      Name of the file containing the PFX certificate for the TLS 1.3     "+CerFile+@"
             or 1.2 protocol without a password. If the path is not specified,
             the certificate is searched for in the folder where the https.net
             server is located and in the root folder containing the domains.
     -p      Port that the server is listening on.                               "+port.ToString()+@"
     -b      Size of read/write buffers.                                         "+bu.ToString()+@"
     -q      Allowable number of requests in the queue.                          "+qu.ToString()+@"
     -q1     Allowed number of requests in the queue per IP.                     "+qu1.ToString()+@"
     -s      Number of requests being processed at the same time. Maximum        "+st.ToString()+@"
             value is "+s9.ToString()+@".
     -s1     Allowed number of simultaneously processed requests per IP.         "+st1.ToString()+@"
     -w      Allowed time to reserve an open channel for request that did not    "+(tw/1000).ToString()+@"
             started. From 1 to "+t9.ToString()+@" seconds.
     -n      Maximum number of dynamically running interpreters or MS VFP        "+db.ToString()+@"
             instances. Processes are launched as needed depending on the
             number of concurrent requests. Maximum value is "+db9.ToString()+@".
     -log    Size of the query log in rows. The log consists of two              "+log9.ToString()+@"
             interleaved versions https.net.x.log and https.net.y.log. If the
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
