# selenium-auth-with-proxy-examples
This repo contains examples of handling various web authentication mechanisms with Selenium via a proxy.
It includes an ASP.NET Core demo web site that implements Basic, Digest, and NTLM authentication.
It includes sample Selenium code using [BenderProxy](https://github.com/jimevans/BenderProxy)
(version 1.1.2 or later) and [PassedBall](https://github.com/jimevans/passedball) (version 1.2.0 
or later) to automate the site. The Selenium code runs in a console application, which will await
you pressing the Enter key before shutting down the proxy and quitting the browser. This will allow
you to see the state of the browser before everything quits. Other features of the sample repo include
working factory classes for Selenium sessions and the demo cases themselves.

To make the demo in the source repo properly work, _**you must run it on Windows**_, because we are
enabling NTLM authentication, and you will need _**administrative access on your Windows machine**_.
This is unfortunate, but there is no other way to get the development web server to listen on a host
name other than "localhost". If you change the test to navigate to the site on "localhost", the
browser will likely bypass the proxy. By default, we are using `www.seleniumhq-test.test` and port
`5000`, but you can use whatever you want. Here's how to configure your test environment so that the
demo app will work properly:

From an elevated ("Run as Administrator") command prompt, edit your hosts file to contain a mapped
entry for the host you wish to use. The hosts file can be edited in any text editor, including Notepad,
so the following command will open it:

    notepad.exe %WinDir%\System32\drivers\etc\hosts

Once open, add the following line:

    127.0.0.1 <host name>

Be sure to substitute your preferred host name for `<host name>` . Save and close the hosts file.

Also in the elevated command prompt, execute the following command:

    netsh http add urlacl url="http://<host name>:<port>/" user=everyone

Once again, be sure to substitute your preferred host name and port for `<host name>` and `<port>`
respectively. You should see a message that the URL reservation was successfully added. Now,
this is a dangerous command, because it does open up a URL reservation for everyone, so you don't
want to leave this permanently in place. You can remove it at any time after you're done using the
sample by using another elevated command prompt to execute:

    netsh http remove urlacl url="http://<host name>:<port>/"

Once you've added the hosts file entry and the URL ACL, you're ready to load and run the
authentication tests. Open the solution in Visual Studio 2019, and you should be able to build
and run. When running, the solution runs a console application that will launch the test web
app, start the proxy server, start a browser configured to use the proxy with Selenium, navigate
to a protected URL for a specific authentication scheme, and then wait for the Enter key to be
pressed. This will let you examine the browser to validate that, yes, the authentication
succeeded. You can also examine the diagnostic output written to the console by the test code,
which describes the `WWW-Authenticate` and `Authorization` headers being used. Once you've
validated to your satisfaction that in the browser really did authenticate using Selenium and
without prompting the user, you can press Enter, which will quit the browser, stop the proxy
server, and shut down the test web app. As an extra validation step, you can also start the
test web app from Visual Studio and manually navigate to the URLs to validate that they really
do prompt for credentials when browsed to.

To change the browser being used ([line 11](https://github.com/jimevans/selenium-auth-with-proxy-examples/blob/master/SeleniumAuthWithProxyExamples/SeleniumAuthWithProxyExamples/Program.cs#L11)),
and the authentication type ([line 15](https://github.com/jimevans/selenium-auth-with-proxy-examples/blob/master/SeleniumAuthWithProxyExamples/SeleniumAuthWithProxyExamples/Program.cs#L15))
being tested by changing the appropriate lines in the main method. If you decided to use a
different host name or port, you can also change that by uncommenting and changing the
appropriate lines ([line 21](https://github.com/jimevans/selenium-auth-with-proxy-examples/blob/master/SeleniumAuthWithProxyExamples/SeleniumAuthWithProxyExamples/Program.cs#L21)
and [line 22](https://github.com/jimevans/selenium-auth-with-proxy-examples/blob/master/SeleniumAuthWithProxyExamples/SeleniumAuthWithProxyExamples/Program.cs#L22),
respectively).

Notes about this code
---------------------
Yes, there is a large amount of duplicated code here. Yes, many of the setup and teardown
methods are identical. No, this is not a bug; it's a feature. This is a demonstration repo,
and is intended to allow one to look at a single class and (mostly) follow the entire flow
without switching to the parent class.

There are binary drivers committed to this repo. These binaries will likely need to be
updated depending on the browser version you're using. There are no guarantees that the
drivers checked into the repo will be kept fully up to date.

Pull requests and issue reports are likely to be low priority. This is not to be callous,
but rather to set accurate expectations on the attention they are likely to receive.
