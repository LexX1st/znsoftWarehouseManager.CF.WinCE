﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace СкладскойУчет
{
    public partial class ОкноИнвентаризацииТоваров : Form
    {

        private Пакеты Обмен;
        private string[][] ОтветСервера;
        List<string[]> КоллекцияСтрок = new List<string[]>();

        private List<СтрокаТаблицыИнвентаризации> ТаблицаИнвентаризации = new List<СтрокаТаблицыИнвентаризации>();
        private List<СтрокаТаблицыЕАН> ТаблицаЕАН = new List<СтрокаТаблицыЕАН>();

        private string Адрес;
        private bool МногоТоваров;

        public ОкноИнвентаризацииТоваров()
        {
            Обмен = new Пакеты("Инвентаризация");
            InitializeComponent();
        }

        // События на форме -----------------------------------------------------------------------------------------------------------------------------------
        private void ОкноИнвентаризацииТоваров_Load(object sender, EventArgs e)
        {
            ОтветСервера = Обмен.ПослатьСтроку("ПолучениеЗаданий");

            if (ОтветСервера == null) return; // в случае ошибки остаться в этом же окне
       
            ЗаполнитьТаблицы(ОтветСервера);

            НадписьАдрес.Text = "Инвентаризация " + Адрес;

            ВывестиТаблицуИнвентаризацииНаЭкран();

            СписокИнвентаризации.Focus();

            // Пытаемся выбрать первую строку
            try
            {
                var ВыбраннаяСтрока = СписокИнвентаризации.Items[0];
                if (ВыбраннаяСтрока == null) return;
                ВыбраннаяСтрока.Selected = true;
                ВыбраннаяСтрока.Focused = true;
            }
            catch (Exception) { }

            ПоказатьДопИнфоТовара();
        }

        private void ОкноИнвентаризацииТоваров_KeyDown(object sender, KeyEventArgs e)
        {

            if (РаботаСоСканером.НажатаКлавишаСкан(e))
            {

                string СтрокаСкан = РаботаСоСканером.Scan();
                if (СтрокаСкан.Length == 0) return;

                e.Handled = true;
                ОбработатьСканТовара(СтрокаСкан);
                return;
            }

            if (e.KeyCode == System.Windows.Forms.Keys.F8 || e.KeyCode == System.Windows.Forms.Keys.Enter)
            {
                e.Handled = true;
                РучнойВводКоличества();
            }

            if (РаботаСоСканером.НажатаПраваяПодэкраннаяКлавиша(e))
            {
                _Далее();
            }

            if (РаботаСоСканером.НажатаЛеваяПодэкраннаяКлавиша(e))
            {
                _Назад();
            }
        }

        private void РучнойВводКоличества()
        {
            var ВыбраннаяСтрока = СписокИнвентаризации.FocusedItem;
            if (ВыбраннаяСтрока == null) return;

            var СтрокаТаблицы = НайтиСтрокуТаблицыИнвентаризацииПоГуиду(ВыбраннаяСтрока.SubItems[2].Text); // Гуид
            if (СтрокаТаблицы == null) return;

            string ТекстИнструкции = "Введите фактическое \nколичество товара";
            ОкноВводКоличества ОкноВводКоличества = new ОкноВводКоличества(ТекстИнструкции, СтрокаТаблицы.Количество, 0, false);
            DialogResult d = ОкноВводКоличества.ShowDialog();
            if (d == DialogResult.OK)
            {
                int Количество = ОкноВводКоличества.Количество_;
                СтрокаТаблицы.Количество = Количество;
                ОбработатьКоличествоСтроки(СтрокаТаблицы);

                // Подтверждаем успешный ввод количества звуком
                РаботаСоСканером.Звук.Ок();
            }
        }

        private void СписокИнвентаризации_SelectedIndexChanged(object sender, EventArgs e)
        {
            ПоказатьДопИнфоТовара();
        }

        public virtual void ПоказатьДопИнфоТовара()
        {
            try
            {
                ДопИнфо.Text = "(" + СписокИнвентаризации.FocusedItem.SubItems[1].Text + ") " + СписокИнвентаризации.FocusedItem.Text; // (Код) Товар
            }
            catch (Exception) { ДопИнфо.Text = ""; }
        }

        private void Назад_Click(object sender, EventArgs e)
        {
            _Назад();
        }

        private void Далее_Click(object sender, EventArgs e)
        {
            _Далее();
        }

        public virtual void _Назад()
        {
            ОтветСервера = Обмен.ПослатьСтроку("ПрерватьЗадания");

            if (ОтветСервера == null) return;

            this.Close();
            return; 
        } 
        // ------------------------------------------------------------------------------------------------------------------------------------------------------


        // Сканирование -----------------------------------------------------------------------------------------------------------------------------------------             
        private void ОбработатьСканТовара(string СтрокаСкан)
        {
            string Код = "";

            // Проверка на ЕАН8 и преобразование к коду по базе
            if (СтрокаСкан.Length == 8)
            {
                var ЕАН8 = ОбщиеФункции.ПроверитьЕАН8(СтрокаСкан);

                if (ЕАН8) // Если символ контрольной суммы верный, преобразуем ЕАН8 к семизначному коду по базе
                {
                    Код = СтрокаСкан.Substring(0, 7);
                }
            }

            var МассивТоваров = НайтиТоварПоЕАН(СтрокаСкан, Код);

            if (МассивТоваров.Count() == 0)
            {
                var ДанныеПолучены = ЗаполнитьТаблицыПоТовару(СтрокаСкан, Код);
                if (!ДанныеПолучены) return;

                // Подтверждаем успешное добавление звуком
                РаботаСоСканером.Звук.Ок();

                МассивТоваров = НайтиТоварПоЕАН(СтрокаСкан, Код);
            }

            string ВыбранныйТовар = null;

            if (МассивТоваров.Count() > 1)
            {
                ВыбранныйТовар = ВыборТовараИзМножества.ВыбратьТоварИзМножества(МассивТоваров);
            }
            else
            {
                ВыбранныйТовар = МассивТоваров.FirstOrDefault()[2];
            }

            if (ВыбранныйТовар == null) return;

            var СтрокаТаблицы = НайтиСтрокуТаблицыИнвентаризацииПоГуиду(ВыбранныйТовар); // Гуид
            if (СтрокаТаблицы == null)
            {
                Инфо.Ошибка("Выбранный товар не найден!");
                return;
            }

            СтрокаТаблицы.Количество += 1;
            ОбработатьКоличествоСтроки(СтрокаТаблицы);
        }

        private IEnumerable<string[]> НайтиТоварПоЕАН(string ЕАН, string Код)
        {
            var СтрокиЕАН = (from Строка in ТаблицаЕАН
                             where Строка.ЕАН == ЕАН || Строка.Код == Код
                             select new { Строка.Код, Строка.Товар, Строка.Гуид }).Distinct();

            var МассивСтрок = СтрокиЕАН.Select(Строка => new string[] { Строка.Код, Строка.Товар, Строка.Гуид });

            return МассивСтрок;
        }
        // -----------------------------------------------------------------------------------------------------------------------------------------------------


        // Завершение Инвентаризации -------------------------------------------------------------------------------------------------------------------------------
        public virtual void _Далее()
        {
            ЗавершениеИнвентаризации();
        }
        
        private void ЗавершениеИнвентаризации()
        {
            // Заполняем массив для отправки в 1с
            КоллекцияСтрок.Clear();

            bool ЕстьСтрокиДляОтправки = false;

            ДобавитьСтроку("Адрес", Адрес);

            foreach (var Строка in ТаблицаИнвентаризации)
            {
                if (Строка.Количество > 0)
                {
                    ЕстьСтрокиДляОтправки = true;
                    ДобавитьСтроку(Строка.Гуид, Строка.Количество.ToString());
                }
            }

            // Если не сосканили ни одного товара и завершаем, надо задать вопрос
            if (!ЕстьСтрокиДляОтправки)
            {
                РаботаСоСканером.Звук.Ошибка();
                var MSGRes = MessageBox.Show("Не сосканирован ни один товар! Вы точно хотите \n завершить \n инвентаризацию ?", "Завершение инвентаризации", MessageBoxButtons.YesNo, MessageBoxIcon.Question, MessageBoxDefaultButton.Button1);
                if (MSGRes != DialogResult.Yes) return;
            }

            Cursor.Current = Cursors.WaitCursor;

            ОтветСервера = Обмен.Послать("ЗавершитьЗадания", КоллекцияСтрок.ToArray());

            Cursor.Current = Cursors.Default;

            //в случае какой либо ошибки при завершении ничего не делаем, даем возможность завершить повторно
            if (ОтветСервера == null) return;

            Form Окно = new ОкноВыбораЗаданийНаИнвентаризацию("ИнвентаризацияСканАдреса", Адрес.Substring(0, 1), Адрес.Substring(0, 3), Адрес.Substring(0, 6), Адрес);
            this.Close();
            Окно.Show();
        }

        void ДобавитьСтроку(params string[] args)
        {
            КоллекцияСтрок.Add(args);
        }  
        // ------------------------------------------------------------------------------------------------------------------------------------------------------


        // Работа с таблицами -----------------------------------------------------------------------------------------------------------------------------------
        private class СтрокаТаблицыИнвентаризации
        {
            public string Товар;
            public string Код;
            public string Гуид;
            public int Количество;
            public ListViewItem СтрокаСписка;
        }

        private class СтрокаТаблицыЕАН
        {
            public string Товар;
            public string Код;
            public string Гуид;
            public string ЕАН;
        }

        private void ЗаполнитьТаблицы(string[][] ОтветСервера)
        {
            ТаблицаЕАН.Clear();
            ТаблицаИнвентаризации.Clear();

            foreach (var Строка in ОтветСервера)
            {
                if (Строка[0] == "Адрес") { Адрес = Строка[1]; continue; }
                if (Строка[0] == "МногоТоваров") { МногоТоваров = Строка[1] == "true"; continue; }

                ДобавитьСтрокуВТаблицы(Строка);
            }
        }

        private bool ЗаполнитьТаблицыПоТовару(string СтрокаСкан, string Код)
        {
            ОтветСервера = Обмен.ПослатьСтроку("СканТовара", СтрокаСкан, Код);

            if (ОтветСервера == null) return false;

            foreach (var Строка in ОтветСервера)
            {
                ДобавитьСтрокуВТаблицы(Строка);        
            }
            return true;
        }

        private void ДобавитьСтрокуВТаблицы(string[] Строка)
        {
            int КоличествоПараметров = Строка.Count();

            // Заполняем таблицу ЕАН
            if (КоличествоПараметров == 4)
            {
                СтрокаТаблицыЕАН СтрокаТаблицы = new СтрокаТаблицыЕАН();

                СтрокаТаблицы.Товар = Строка[0];
                СтрокаТаблицы.Код = Строка[1];
                СтрокаТаблицы.Гуид = Строка[2];
                СтрокаТаблицы.ЕАН = Строка[3];

                ТаблицаЕАН.Add(СтрокаТаблицы);
            }
            else // Заполняем таблицу инвентаризации
            {
                СтрокаТаблицыИнвентаризации СтрокаТаблицы = new СтрокаТаблицыИнвентаризации();

                СтрокаТаблицы.Товар = Строка[0];
                СтрокаТаблицы.Код = Строка[1];
                СтрокаТаблицы.Гуид = Строка[2];
                СтрокаТаблицы.Остаток = Строка[3];
                СтрокаТаблицы.Количество = Строка[4];

                ТаблицаИнвентаризации.Add(СтрокаТаблицы);
            }
        }

        private void ВывестиТаблицуИнвентаризацииНаЭкран()
        {
            СписокИнвентаризации.Items.Clear();

            if (МногоТоваров) return;
      
            foreach (var Строка in ТаблицаИнвентаризации)
            {
                ДобавитьСтрокуНаЭкран(Строка);
            }

        }

        private void ДобавитьСтрокуНаЭкран(СтрокаТаблицыИнвентаризации Строка)
        {
            ListViewItem НоваяСтрока = new ListViewItem();
            НоваяСтрока.Text = Строка.Товар;
            НоваяСтрока.SubItems.Add(Строка.Код);
            НоваяСтрока.SubItems.Add(Строка.Гуид);
            НоваяСтрока.SubItems.Add(Строка.Количество.ToString());

            СписокИнвентаризации.Items.Add(НоваяСтрока);

            Строка.СтрокаСписка = НоваяСтрока;
        }

        private СтрокаТаблицыИнвентаризации НайтиСтрокуТаблицыИнвентаризацииПоГуиду(string Гуид)
        {
            var СтрокаТаблицы = from Строка in ТаблицаИнвентаризации
                                where Строка.Гуид == Гуид
                                select Строка;
            if (СтрокаТаблицы.Count() == 0) return null;
            return СтрокаТаблицы.First();
        }

        private void ОбработатьКоличествоСтроки(СтрокаТаблицыИнвентаризации Строка)
        {        
            // Если строки на экране нет, добавляем ее
            if (Строка.СтрокаСписка == null)
            {
                ДобавитьСтрокуНаЭкран(Строка);
            }

            var СтрокаНаЭкране = Строка.СтрокаСписка;

            СтрокаНаЭкране.SubItems[3].Text = Строка.Количество.ToString();
            СписокИнвентаризации.EnsureVisible(СтрокаНаЭкране.Index);

            foreach (int index in СписокИнвентаризации.SelectedIndices)
            {
                СписокИнвентаризации.Items[index].Selected = false;
            }
            СтрокаНаЭкране.Selected = true;

            СтрокаНаЭкране.Focused = true;
            ПоказатьДопИнфоТовара();
        }
        // ------------------------------------------------------------------------------------------------------------------------------------------------------
    }
}