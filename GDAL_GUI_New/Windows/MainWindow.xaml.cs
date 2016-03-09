﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Data;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using Gat.Controls;

namespace GDAL_GUI_New
{
    /// <summary>
    /// Логика взаимодействия для MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        // Переменные
                #region Переменные
        // Коллекция задач
        private ObservableCollection<MyTask> m_Tasks;
        // Счётчик количества добавленных задач
        private int m_TasksCounter;
        // Текущая (выбранная) задача 
        private MyTask m_CurrentTask;
        // StringBuilder, в который будет сохраняться выходная информация от утилит
        private MyStringBuilder m_OutputStringBuilder;

        #endregion

        // Конструкторы
                #region Конструкторы

        public MainWindow()
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            m_Tasks = new ObservableCollection<MyTask>();
            m_TasksCounter = 0;
            m_CurrentTask = null;
            m_OutputStringBuilder = new MyStringBuilder();

            TaskManager.InitializeProcessManager(this);
            TaskManager.SetDataReceivedHandler = OutputDataRecieved;

            // Привязка StringBuilder к TextBlock
            Binding myBinding = new Binding();
            myBinding.Path = new PropertyPath("Text");
            myBinding.Mode = BindingMode.OneWay;
            myBinding.Source = m_OutputStringBuilder;
            myBinding.UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged;
            TextBox_OutputData.SetBinding(TextBox.TextProperty, myBinding);

            EventAndPropertiesInitialization();
        }
        #endregion

        // Свойства
                #region Свойства
        // Выдаёт коллекцию задач
        public ObservableCollection<MyTask> GetTasksList
        {
            get { return m_Tasks; }
        }
        // Выдаёт количество добавленных задач
        public int GetTasksCounter
        {
            get { return m_TasksCounter; }
        }
        // Выдаёт и задаёт текущую (выбранную) задачу
        public MyTask CurrentTask
        {
            get
            {
                return m_CurrentTask;
            }
            set
            {
                    m_CurrentTask = value;
            }
        }
        
        #endregion

        // Методы
                #region Методы
        // Подписка на события и другие инициализации
        private void EventAndPropertiesInitialization()
        {
            // Подписка на события
            Menu_File_Exit.Click += Menu_File_Exit_Click;
            Menu_Edit_AddTask.Click += Menu_Edit_AddTask_Click;
            Menu_Edit_EditSelectedTask.Click += Menu_Edit_EditSelectedTask_Click;
            Menu_Edit_RemoveSelectedTask.Click += Menu_Edit_RemoveSelectedTask_Click;
            Menu_Edit_RemoveAllTasks.Click += Menu_Edit_RemoveAllTasks_Click;
            Menu_Run_RunAll.Click += Menu_Run_RunAll_Click;
            Menu_Run_RunSelected.Click += Menu_Run_RunSelected_Click;
            Menu_Output_Clear.Click += Menu_Output_Clear_Click;
            Menu_Output_SaveToFile.Click += Menu_Output_SaveToFile_Click;
            Menu_Settings.Click += Menu_Settings_Click;
            Menu_About.Click += Menu_About_Click;
            Menu_Help.Click += Menu_Help_Click;

            m_Tasks.CollectionChanged +=
                new System.Collections.Specialized.NotifyCollectionChangedEventHandler(Tasks_CollectionChanged);
        }

        public void SendMessageToTextBox(string message)
        {
            m_OutputStringBuilder.Text =
                Environment.NewLine + message;
        }

        // Добавляет переданную задачу и инкремирует счётчик
        public void AddNewTask(MyTask task)
        {
            if (task == null)
            {
                return;
            }
            m_Tasks.Add(task);
            m_TasksCounter++;
        }

        public void EditSelectedTask(MyTask task)
        {
            if (m_Tasks != null && m_Tasks.Count > 0 && task != null)
            {
                TaskEditWindow taskEditWindow = new TaskEditWindow(this, task);
                taskEditWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Нечего редактировать.");
            }
        }

        public void ReplaceEditedTask(MyTask task)
        {
            if (task == null)
            {
                return;
            }
            if (m_CurrentTask == task)
            {
                m_CurrentTask = null;
                task.GetTaskElement.IsCurrent = false;
            }
            int index = m_Tasks.IndexOf(m_Tasks.FirstOrDefault(x => x.GetTaskID == task.GetTaskID));
            task.GetTaskElement.SetTaskElementState(TaskElement.TaskElementState.Normal);
            task.GetTaskElement.SetPreviousState();
            
            //m_Tasks.Insert(index, task);
            //m_Tasks.Remove(m_Tasks.Where(x => x.GetTaskID == task.GetTaskID).First());
            //m_Tasks.Add(task);

            m_Tasks.RemoveAt(index);
            m_Tasks.Insert(index, task);
        }

        public void RemoveTask(MyTask task)
        {
            if (m_Tasks != null && m_Tasks.Count > 0 && task != null)
            {
                if (m_CurrentTask == task)
                {
                    m_Tasks.Remove(m_CurrentTask);
                    m_CurrentTask = null;
                    MessageBox.Show("Выбранная задача удалена.", "Успех!",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    m_Tasks.Remove(task);
                    MessageBox.Show("Задача удалена.", "Успех!",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            else
            {
                MessageBox.Show("Нечего удалять.");
            }
        }

        public void RunSelectedTask(MyTask task)
        {
            if (task == null)
            {
                MessageBox.Show("Не выбрана задача!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            TaskManager.TasksCollection = m_Tasks;
            TaskManager.RunSelected(task);
        }
        #endregion

        // Обработчики событий
        #region Обработчики событий
        // Выход из приложения по нажатию на кнопку Выход в меню
        private void Menu_File_Exit_Click(object sender, RoutedEventArgs e)
        {
            Application.Current.Shutdown(0);
        }
        // Добавление нового задания
        private void Menu_Edit_AddTask_Click(object sender, RoutedEventArgs e)
        {
            TaskEditWindow taskEditWindow = new TaskEditWindow(this);
            taskEditWindow.ShowDialog();
        }
        // Редактирование выбранной задачи
        private void Menu_Edit_EditSelectedTask_Click(object sender, RoutedEventArgs e)
        {
            EditSelectedTask(m_CurrentTask);
            /*
            if (m_Tasks != null && m_Tasks.Count > 0 && m_CurrentTask != null)
            {
                TaskEditWindow taskEditWindow = new TaskEditWindow(this, m_CurrentTask);
                taskEditWindow.ShowDialog();
            }
            else
            {
                MessageBox.Show("Нечего редактировать.");
            }
            */
        }
        // Удаление выбранной задачи
        private void Menu_Edit_RemoveSelectedTask_Click(object sender, RoutedEventArgs e)
        {
            RemoveTask(m_CurrentTask);
            /*
            //MessageBox.Show("Заглушка. Menu_Edit_RemoveSelectedTask_Click");
            if (m_Tasks != null && m_Tasks.Count > 0 && m_CurrentTask != null)
            {
                m_Tasks.Remove(m_CurrentTask);
                m_CurrentTask = null;
                MessageBox.Show("Выбранная задача удалена.", "Успех!", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нечего удалять.");
            }
            */
        }
        // Удаление всех задач
        private void Menu_Edit_RemoveAllTasks_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Заглушка. Menu_Edit_RemoveAllTasks_Click");
            if (m_Tasks != null && m_Tasks.Count > 0 )
            {
                m_Tasks.Clear();
                m_CurrentTask = null;
                StackPanel_TaskElements.Children.Clear();
                MessageBox.Show("Все задачи удалены.", "Успех!",
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
            else
            {
                MessageBox.Show("Нечего удалять.");
            }
        }
        // Запуск всех добавленных заданий по порядку
        private void Menu_Run_RunAll_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Заглушка. Run All");
            if (m_Tasks != null)
            {
                foreach (MyTask task in m_Tasks)
                {
                    task.GetTaskElement.SetTaskElementState(TaskElement.TaskElementState.Normal);
                }
            }
            TaskManager.TasksCollection = m_Tasks;
            TaskManager.RunAll();
        }
        // Запуск одного выбранного задания
        private void Menu_Run_RunSelected_Click(object sender, RoutedEventArgs e)
        {
            RunSelectedTask(m_CurrentTask);
            m_CurrentTask = null;
            /*
            if (m_CurrentTask == null)
            {
                MessageBox.Show("Не выбрана задача!", "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            TaskManager.TasksCollection = m_Tasks;
            TaskManager.RunSelected(m_CurrentTask);
            */
        }
        // Сохранение данных из окна вывода (сохранение логов) в файл
        private void Menu_Output_SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            //MessageBox.Show("Заглушка. Menu_Output_SaveToFile_Click");
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                FilterIndex = 0,
                FileName = String.Format("GDAL_GUI_{0}_{1}_{2}_-_{3}_{4}_{5}_Output_Log", 
                    DateTime.Now.Day, DateTime.Now.Month, DateTime.Now.Year, 
                    DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second)
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    StreamWriter streamWriter = new StreamWriter(saveFileDialog.FileName);
                    streamWriter.Write(m_OutputStringBuilder.Text);
                    streamWriter.Close();
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось сохранить данные из окна вывода.", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
        // Очистка окна вывода
        private void Menu_Output_Clear_Click(object sender, RoutedEventArgs e)
        {
            m_OutputStringBuilder.Clear();
            TextBox_OutputData.Clear();
        }
        // Открытие окна настроек
        private void Menu_Settings_Click(object sender, RoutedEventArgs e)
        {
            SettingsWindow settingsWindow = new SettingsWindow();
            settingsWindow.ShowDialog();
        }
        // Открытие окна "О программе"
        private void Menu_About_Click(object sender, RoutedEventArgs e)
        {
            About about = new About();
            about.Title = "GDAL GUI";
            about.AdditionalNotes = String.Empty;
            about.Show();
            
        }

        private void Menu_Help_Click(object sender, RoutedEventArgs e)
        {
            Process helpProcess = new Process();
            if (File.Exists(@"Resources\Help\GDAL_GUI_Help.chm"))
            {
                helpProcess.StartInfo.FileName = @"Resources\Help\GDAL_GUI_Help.chm";
                helpProcess.Start();
            }
            else
            {
                MessageBox.Show("Не удалось найти файл справки: " +
                     @"Resources\Help\GDAL_GUI_Help.chm");
                return;
            }
            
        }

        // Обработчик события Изменения коллекции задач (для ObservableCollection<MyTask> m_Tasks)
        private void Tasks_CollectionChanged(object sender,
            System.Collections.Specialized.NotifyCollectionChangedEventArgs e)
        {
            if (e.NewItems != null && e.NewItems[0] != null)
            {
                MyTask task = e.NewItems[0] as MyTask;
                //StackPanel_TaskElements.Children.Add(task.GetTaskElement);
                StackPanel_TaskElements.Children.Insert(e.NewStartingIndex, task.GetTaskElement);
            }
            else if (e.OldItems != null && e.OldItems[0] != null)
            {
                MyTask task = e.OldItems[0] as MyTask;
                StackPanel_TaskElements.Children.Remove(task.GetTaskElement);
            }
        }
        // Обработчик события Получения новых данных вывода запущенного процесса
        private void OutputDataRecieved(object sender, DataReceivedEventArgs e)
        {
            if (!String.IsNullOrEmpty(e.Data))
            {
                m_OutputStringBuilder.Text = Environment.NewLine + e.Data;
            }
        }

        #endregion
    }


}
