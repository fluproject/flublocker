using System;
using System.Collections.Generic;
using System.Text;
using System.Web;

namespace FluBlocker
{
    public class FluBlocker : IHttpModule
    {
        string blockFile = Environment.GetEnvironmentVariable("HOMEDRIVE") + "\\block.flu";

        public void Init(HttpApplication application)
        {
            application.EndRequest += new EventHandler(application_EndRequest);
        }

        /// <summary>
        /// Lee un fichero de texto y devuelve una lista de string con una linea en cada nodo
        /// </summary>
        /// <param name="fichero"></param>
        /// <returns></returns>
        List<string> readFile(string fichero)
        {
            List<string> lista = new List<string>();   //Guardaremos cada linea del fichero en un string de la lista
            string line;
            System.IO.StreamReader file = new System.IO.StreamReader(fichero);
            while ((line = file.ReadLine()) != null)
            {
                //Si la linea no es un comentario, o no está vacia la añadimos
                if ((!line.Contains("#")) && (line.Length > 1))
                    lista.Add(line);
            }
            file.Close();
            return lista;
        }

        /// <summary>
        /// Busca en una lista pasada por cabecera si existe una ip, un user-agent, un puerto o un método
        /// </summary>
        /// <param name="type">ip, user-agent, puerto o método</param>
        /// <param name="lista">listado de elementos bloqueados</param>
        /// <returns></returns>
        bool searchList(string type, List<string> lista)
        {
            bool existe = false;

            //Vamos eliminando los nodos de la lista que vamos leyendo
            lista.RemoveAt(0);

            while ((!lista[0].Contains("*")) && (!existe))
            {
                existe = type.Contains(lista[0]);
                lista.RemoveAt(0);
            }
            return (existe);
        }

        /// <summary>
        /// Función que se ejecutará al recibir una petición al servidor para bloquear la conexión si se estimase necesario
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        public void application_EndRequest(object sender, EventArgs e)
        {
            HttpApplication application = (HttpApplication)sender;
            HttpContext context = application.Context;

            try
            {
                //Recuperamos los datos del fichero de configuración
                List<string> lista = readFile(blockFile);

                //Recuperamos el user-agent, puerto origen, IP y método de conexión
                string userAgent = context.Request.UserAgent;
                string srcPort = context.Request.ServerVariables.Get("REMOTE_PORT");
                string ip = context.Request.UserHostAddress;
                string method = context.Request.HttpMethod;

                //Filtro por IP
                bool ConnectionRefused = searchList(ip, lista);

                //Filtro por USER-AGENT
                if (!ConnectionRefused)
                    ConnectionRefused = searchList(userAgent, lista);

                //Filtro por MÉTODO
                if (!ConnectionRefused)
                    ConnectionRefused = searchList(method, lista);

                //Filtro por PUERTO
                if (!ConnectionRefused)
                    ConnectionRefused = searchList(srcPort, lista);

                //Si le encontramos en la lista, le bloqueamos la conexión
                if (ConnectionRefused)
                {
                    context.Response.Clear();
                    context.Response.Close();
                }
            }
            catch { }
        }

        public void Dispose() { }
    }
}
