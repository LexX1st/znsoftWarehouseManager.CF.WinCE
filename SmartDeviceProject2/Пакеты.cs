﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using СкладскойУчет.СсылкаНаСервис;
namespace СкладскойУчет
{
    class Пакеты
    {
        public СоединениеВебСервис Соединение;
        public List<СтрокаНоменклатуры> Список = new List<СтрокаНоменклатуры>();
        public СтрокаНоменклатуры[] СписокСтрок;
        public СтрокаНоменклатуры[] ОтветСервера;
        public string Операция;
        
        public Пакеты(string операция) {

            this.Операция = операция;
            Соединение = СоединениеВебСервис.ПолучитьСервис();
        }

        public СтрокаНоменклатуры[] ПодготовитьСтроку(string Код, string Наименование, int Количество)
        {
            СписокСтрок = new СтрокаНоменклатуры[1]{new СтрокаНоменклатуры()};
            СписокСтрок[0].Код = Код;
            СписокСтрок[0].Наименование = Наименование;
            СписокСтрок[0].Количество = String.Format("{0}", Количество);
            return СписокСтрок;
        }

        public СтрокаНоменклатуры[] Послать()
        {
                ОтветСервера = Соединение.Сервис.ОбменТСД(Операция, СписокСтрок);

            return ОтветСервера;

        }

        public СтрокаНоменклатуры[] ПослатьСтроку(string Код, string Наименование, int Количество)
        {
            try
            {
                ПодготовитьСтроку(Код, Наименование, Количество);
                return Послать();
            }
            catch (System.Exception e)
            {
                Ошибка ОкноОшибки = new Ошибка(e.Message);
                ОкноОшибки.ShowDialog();

                return null;

            }
            //catch (System.Net.WebException we)
            //{
            //    var r = (System.Net.HttpWebResponse)we.Response;
            //    r.StatusCode = System.Net.HttpStatusCode.Unauthorized;
            //   //we.Status = ()System.Net.WebExceptionStatus 
            //}
        }

    }
}
