﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using Binding = System.Windows.Data.Binding;
using Button = System.Windows.Controls.Button;
using ComboBox = System.Windows.Controls.ComboBox;
using DataGrid = System.Windows.Controls.DataGrid;
using GroupBox = System.Windows.Controls.GroupBox;
using HorizontalAlignment = System.Windows.HorizontalAlignment;
using Label = System.Windows.Controls.Label;
using MessageBox = System.Windows.MessageBox;
using MouseEventArgs = System.Windows.Input.MouseEventArgs;
using OpenFileDialog = Microsoft.Win32.OpenFileDialog;
using RadioButton = System.Windows.Controls.RadioButton;
using SaveFileDialog = Microsoft.Win32.SaveFileDialog;
using TextBox = System.Windows.Controls.TextBox;

namespace GDAL_GUI_New
{
    /// <summary>
    /// Логика взаимодействия для TaskEditWindow.xaml
    /// </summary>
    public partial class TaskEditWindow : Window, INotifyPropertyChanged
    {
        // Переменные
            #region Переменные
        private MainWindow m_MainWindow;
        private MyTask m_Task;
        private List<string> m_UtilitiesNames;
        private List<MyDataRow> m_UtilityParameters;
        private DataRow m_UtilityInfo;
        // Этот флаг используется при закрытии данного окна, чтобы выводить/не выводить 
        // диалог-предупреждение, что изменения не будут сохранены
        private bool m_IsThisTaskAdded;
        //private string[] m_InputFiles;
        private List<string> m_InputFiles;
        private string[] m_ThumbnailsPaths;
        private string m_OutputPath;
        private string m_FormedParametersArgument;
        private Process m_ProcessForVersion;
        private Version m_UtilityVersion;
        public enum InputMode
        {
            OneFile,
            MultipleFilesForOneTask,
            MultipleFilesForMultipleTasks,
            FromAnotherTasksForOneTask,
            FromAnotherTasksForMultipleTasks
        };
        private InputMode m_CurrentMode;
        private List<DataTable> m_AdditionalParametersInputs;
        private string[] m_AllParameters;
        // Списки, необходимые для параметров типа Options
        private List<string> m_AvailableOptionsTypes;
        private List<string> m_AvailableGroups;
        private List<DataRow> m_AvailableKeysWithValues;
        private List<string> m_AvailableKeys;
        private List<string> m_AvailableValues;

        public event PropertyChangedEventHandler PropertyChanged;

        public enum TaskEditWindowMode
        {
            NewTask,
            EditingExistingTask
        }
        private TaskEditWindowMode m_TaskEditWindowMode;

        private string m_InputFilter =
                "TIFF/GeoTIFF Files (*.tiff;*.tif)|*.tiff;*.tif|JPEG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|" +
                "GIF Files (*.gif)|*.gif|IMG Files (*.img)|*.img|ESRI/ENVI/GenBin (*.hdr)|*.hdr|VRT Files (*.vrt)|*.vrt|All Files (*.*)|*.*";
        private string m_OutputFilter =
                "TIFF/GeoTIFF Files (*.tif)|*.tif|JPEG Files (*.jpg)|*.jpg|PNG Files (*.png)|*.png|BMP Files (*.bmp)|*.bmp|" +
                "GIF Files (*.gif)|*.gif|IMG Files (*.img)|*.img|ESRI/ENVI/GenBin (*.hdr)|*.hdr|VRT Files (*.vrt)|*.vrt|Other Files (*.*)|*.*";

        private Dictionary<InputMode, string> m_InputModeChoices;
        #endregion

        // Конструкторы
        #region Конструкторы
        public TaskEditWindow(MainWindow mainWindow)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            
            m_MainWindow = mainWindow;
            m_Task = new MyTask(m_MainWindow);
            m_IsThisTaskAdded = false;
            m_UtilitiesNames = new List<string>();
            m_UtilityParameters = new List<MyDataRow>();
            m_CurrentMode = InputMode.OneFile;
            m_FormedParametersArgument = String.Empty;
            m_AdditionalParametersInputs = new List<DataTable>();
            m_TaskEditWindowMode = TaskEditWindowMode.NewTask;
            m_InputModeChoices = new Dictionary<InputMode, string>();
            m_InputFiles = new List<string>();

            // Инициализируем экземпляр процесса, чтобы узнавать версии утилит
            m_ProcessForVersion = new Process();
            m_ProcessForVersion.StartInfo.UseShellExecute = false;
            m_ProcessForVersion.StartInfo.CreateNoWindow = true;
            m_ProcessForVersion.StartInfo.RedirectStandardOutput = true;
            m_ProcessForVersion.StartInfo.Arguments = "--version";
            m_ProcessForVersion.OutputDataReceived += new DataReceivedEventHandler(ProcessForVerion_DataReceived);

            EventAndPropertiesInitialization();
            ConnectToDbAndGetNecessaryData();
        }

        public TaskEditWindow(MainWindow mainWindow, MyTask task)
        {
            InitializeComponent();
            this.WindowStartupLocation = WindowStartupLocation.CenterScreen;

            m_MainWindow = mainWindow;
            m_Task = task;
            m_IsThisTaskAdded = false;
            m_UtilitiesNames = new List<string>();
            m_UtilityParameters = new List<MyDataRow>();
            m_CurrentMode = InputMode.OneFile;
            m_FormedParametersArgument = String.Empty;
            m_AdditionalParametersInputs = new List<DataTable>();
            m_TaskEditWindowMode = TaskEditWindowMode.EditingExistingTask;
            m_InputModeChoices = new Dictionary<InputMode, string>();
            m_InputFiles = new List<string>();

            // Инициализируем экземпляр процесса, чтобы узнавать версии утилит
            m_ProcessForVersion = new Process();
            m_ProcessForVersion.StartInfo.UseShellExecute = false;
            m_ProcessForVersion.StartInfo.CreateNoWindow = true;
            m_ProcessForVersion.StartInfo.RedirectStandardOutput = true;
            m_ProcessForVersion.StartInfo.Arguments = "--version";
            m_ProcessForVersion.OutputDataReceived += new DataReceivedEventHandler(ProcessForVerion_DataReceived);

            EventAndPropertiesInitialization();
            ConnectToDbAndGetNecessaryData();
            RestorePreviousStateOfTask();
        }


        #endregion

        // Свойства
            #region Свойства

        public string OutputFilePath
        {
            get { return m_OutputPath; }
            set
            {
                if (!String.IsNullOrEmpty(value))
                {
                    m_OutputPath = value;
                    OnPropertyChanged("OutputFilePath");
                }
            }
        }
        #endregion

        // Методы
            #region Методы
        private void EventAndPropertiesInitialization()
        {
            // Подписка на события
            this.Closing += 
                ThisWindow_Closing;
            Button_BrowseInputFile.Click +=
                Button_BrowseInputFile_Click;
            Button_BrowseOutputFile.Click +=
                Button_BrowseOutputFile_Click;
            TaskEdit_Menu_AddTask.Click += 
                TaskEdit_Menu_AddTask_Click;
            TaskEdit_Menu_Exit.Click += 
                TaskEdit_Menu_ExitWithoutAdding_Click;
            ComboBox_UtilitiesNames.SelectionChanged += 
                ComboBox_UtilitiesNames_SelectionChanged;
            ListBox_AvailableParameters.SelectionChanged +=
                new SelectionChangedEventHandler(ListBox_AvailableParameters_SelectionChanged);
            ComboBox_InputOptions.SelectionChanged +=
                ComboBox_InputOptions_SelectionChanged;
            Button_FileList_MoveUp.Click +=
                Button_FileList_MoveUp_Click;
            Button_FileList_MoveDown.Click +=
                Button_FileList_MoveDown_Click;


            Binding outputBinding = new Binding()
            {
                Path = new PropertyPath("OutputFilePath"),
                Mode = BindingMode.TwoWay,
                Source = this,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged
            };
            TextBox_OutputFile.SetBinding(TextBox.TextProperty, outputBinding);

            // Дополнительная инициализация параметров объектов
            m_InputModeChoices.Add(InputMode.OneFile,
                FindResource("m_RadioButton_InputMode_TextBlock_OneFile").ToString());
            m_InputModeChoices.Add(InputMode.MultipleFilesForOneTask,
                FindResource("m_RadioButton_InputMode_TextBlock_MultipleFilesForOneTask").ToString());
            m_InputModeChoices.Add(InputMode.MultipleFilesForMultipleTasks,
                FindResource("m_RadioButton_InputMode_TextBlock_MultipleFilesForMultipleTasks").ToString());
            m_InputModeChoices.Add(InputMode.FromAnotherTasksForOneTask,
                FindResource("m_RadioButton_InputMode_TextBlock_FromAnotherTasksForOneTask").ToString());
            m_InputModeChoices.Add(InputMode.FromAnotherTasksForMultipleTasks,
                FindResource("m_RadioButton_InputMode_TextBlock_FromAnotherTasksForMultipleTasks").ToString());
            ComboBox_InputOptions.ItemsSource = m_InputModeChoices;
            ComboBox_InputOptions.SelectedIndex = 0;
        }

        // Подключение к базе данных и получение доступных утилит 
        // (проверяются утилиты в базе данных и в указанной папке)
        private void ConnectToDbAndGetNecessaryData()
        {
            if (DataBaseControl.ConnectToDB() == true)
            {
                try
                {
                    // Инициализируем переменную с количеством доступных утилит
                    int numOfAvailableUtilities = 0;
                    numOfAvailableUtilities = CheckAvailableUtilities.GetNumOfAvailableUtilities(
                        Properties.Settings.Default.UtilitiesDirectory,
                        DataBaseControl.GetUtilitiesNames());
                    // Если есть доступные утилиты, то получаем список доступных утилит
                    if (numOfAvailableUtilities > 0)
                    {
                        m_UtilitiesNames = CheckAvailableUtilities.GetListOfAvailableUtilities(
                                                Properties.Settings.Default.UtilitiesDirectory,
                                                DataBaseControl.GetUtilitiesNames());
                        ComboBox_UtilitiesNames.ItemsSource = m_UtilitiesNames;
                        ComboBox_UtilitiesNames.SelectedIndex = 0;
                    }
                    else
                    {
                        MessageBox.Show("В указанной папке не найдены поддерживаемые утилиты.\n" +
                                        "Пожалуйста, выберите корректную папку с утилитами в настройках.",
                            "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                        //this.Close();
                        this.Grid_TaskEditWindow.IsEnabled = false;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show(
                        "Произошла ошибка. Не удалось получить список доступных утилит." +
                        Environment.NewLine + ex.Data,
                        "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                }

                //m_UtilitiesNames = DataBaseControl.GetUtilitiesNames();
                //ComboBox_UtilitiesNames.ItemsSource = m_UtilitiesNames;
                //ComboBox_UtilitiesNames.SelectedIndex = 0;
            }
            else
            {
                return;
            }
        }

        //protected virtual void OnPropertyChanged(string propertyName = null)
        private void OnPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, new PropertyChangedEventArgs(propertyName));
        }

        private void ParameterAdded(SelectionChangedEventArgs e)
        {
            // Получаем отмеченный параметр
            MyDataRow currentParameter = e.AddedItems[0] as MyDataRow;
            // Если для этого параметра есть дополнительные параметры, то
            // добавим элементы ввода для этих дополнительных параметров в StackPanel
            if ((bool)currentParameter.GetDataRow["IsThereAdditionalParameters"] == true)
            {
                // Создаём GroupBox, в котором будут храниться элементы ввода
                GroupBox gB = new GroupBox()
                {
                    Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                    Header = currentParameter.GetDataRow["NameOfTheParameter"].ToString()
                };
                Grid grid = new Grid();
                // Получаем все дополнительные параметры
                string[] additionalParameters =
                        currentParameter.GetDataRow["AdditionalParameters"].ToString().Split(
                            new char[2] { ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                // Проверяем, должен ли пользователь вводить эти параметры  вручную.
                // Если да, то создаём поля для ввода
                if (currentParameter.GetDataRow["AdditionalParametersType"].ToString() == "ManualInput")
                {
                    // Проверяем, можно ли этот параметр вызвать несколько раз
                    if ((bool) currentParameter.GetDataRow["MultipleCalls"] == true)
                    {
                        // Создаём определения строк и столбцов для сетки в GroupBox
                        RowDefinition rowDataGrid = new RowDefinition();
                        RowDefinition rowButtonAdd = new RowDefinition();
                        RowDefinition rowButtonRemove = new RowDefinition();
                        grid.RowDefinitions.Add(rowDataGrid);
                        grid.RowDefinitions.Add(rowButtonAdd);
                        grid.RowDefinitions.Add(rowButtonRemove);
                        // Создаём экземпляр DataGrid, в который будем вводить дополнительные параметры
                        DataGrid dataGrid = new DataGrid()
                        {
                            Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            GridLinesVisibility = DataGridGridLinesVisibility.All,
                            CanUserAddRows = false
                        };
                        // Создаём таблицу, в которой будут храниться введённые 
                        // значения дополнительных параметров
                        DataTable table = new DataTable()
                        {
                            TableName = currentParameter.GetDataRow["NameOfTheParameter"].ToString()
                        };
                        // Для каждого дополнительного параметра создаём столбец в таблице
                        foreach (string parameter in additionalParameters)
                        {
                            DataColumn column = new DataColumn()
                            {
                                Caption = parameter,
                                ColumnName = parameter
                            };
                            table.Columns.Add(column);
                        }
                        // Добавляем созданную таблицу в список таблиц для доп. параметров
                        // и назначаем экземпляру DataGrid в качестве источника эту таблицу
                        // из списка
                        m_AdditionalParametersInputs.Add(table);
                        dataGrid.ItemsSource = m_AdditionalParametersInputs.Last<DataTable>().DefaultView;
                        
                        // Создаём кнопку, при нажатии на которую будет добавляться новая строка
                        // в DataGrid
                        Button buttonAddRow = new Button()
                        {
                            Content = "Add new row",
                            Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom
                        };
                        // Добавляем этой кнопке обработчик события
                        buttonAddRow.Click += new RoutedEventHandler(Button_AddRow_Click);
                        // Создаём кнопку, при нажатии на которую будет удаляться 
                        // выбранная строка в DataGrid
                        Button buttonRemoveRow = new Button()
                        {
                            Content = "Remove selected row",
                            Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                            HorizontalAlignment = HorizontalAlignment.Right,
                            VerticalAlignment = VerticalAlignment.Bottom
                        };
                        // Добавляем этой кнопке обработчик события
                        buttonRemoveRow.Click += new RoutedEventHandler(Button_RemoveRow_Click);
                        // Формируем GroupBox и добавляем его в StackPanel
                        Grid.SetRow(dataGrid, 0);
                        Grid.SetRow(buttonAddRow, 1);
                        Grid.SetRow(buttonRemoveRow, 2);
                        grid.Children.Add(dataGrid);
                        grid.Children.Add(buttonAddRow);
                        grid.Children.Add(buttonRemoveRow);
                        gB.Content = grid;
                        StackPanel_AdditionalParameters.Children.Add(gB);
                    }
                    // Если этот параметр может вызываться только единожды
                    else
                    {
                        // Переменная-счётчик для строк (GridRow) в сетке (Grid)
                        int rowCount = 0;
                        // Для каждого дополнительного параметра добавляем две строки 
                        // в сетке (для Label и TextBox) и сами элементы: Label и TextBox
                        foreach (string parameter in additionalParameters)
                        {
                            // Добавляем две строки в сетку
                            grid.RowDefinitions.Add(new RowDefinition());
                            grid.RowDefinitions.Add(new RowDefinition());
                            // Создаём экземпляр Label
                            Label label = new Label()
                            {
                                Name = "Label_" + parameter,
                                Content = parameter,
                                VerticalAlignment = VerticalAlignment.Top,
                                HorizontalAlignment = HorizontalAlignment.Left
                            };
                            // Создаём экземпляр TextBox
                            TextBox textBox = new TextBox()
                            {
                                Name = "TextBox_" + parameter,
                                Tag = parameter,
                                VerticalAlignment = VerticalAlignment.Bottom,
                                HorizontalAlignment = HorizontalAlignment.Stretch
                            };
                            // Устанавливаем для Label и TextBox индексы добавленных строк
                            // (т.е. в каких строках в Grid элементы находятся)
                            // (прямо при добавлении сразу инкрементируем счётчик)
                            Grid.SetRow(label, rowCount++);
                            Grid.SetRow(textBox, rowCount++);
                            //rowCount++;
                            // Добавляем созданные элементы в Grid
                            grid.Children.Add(label);
                            grid.Children.Add(textBox);
                        }
                        // Добавляем Grid в GroupBox
                        gB.Content = grid;
                        // Добавляем GroupBox в StackPanel
                        StackPanel_AdditionalParameters.Children.Add(gB);
                    }
                }
                // Если параметр требует от пользователя выбрать один из вариантов
                else if (currentParameter.GetDataRow["AdditionalParametersType"].ToString() == "Selecting")
                {
                    // Создаём новый ComboBox, в котором будeт храниться доступные варианты
                    ComboBox cB = new ComboBox()
                    {
                        IsEditable = true,
                        // Возможно это (тэг) не потребуется
                        //Tag = currentParameter.GetDataRow["NameOfTheParameter"]
                        Tag = "selected_value"
                    };
                    // Добавляем источник данных для ComboBox и добавляем его 
                    // в GroupBox и затем в StackPanel
                    cB.ItemsSource = additionalParameters;
                    gB.Content = cB;
                    StackPanel_AdditionalParameters.Children.Add(gB);
                }
                else if (currentParameter.GetDataRow["AdditionalParametersType"].ToString() == "OpenFile")
                {
                    ColumnDefinition textBoxColumn = new ColumnDefinition();
                    ColumnDefinition buttonColumn = new ColumnDefinition()
                    {
                        Width = new GridLength(75, GridUnitType.Pixel)
                    };
                    grid.ColumnDefinitions.Add(textBoxColumn);
                    grid.ColumnDefinitions.Add(buttonColumn);
                    // Переменная-счётчик для строк (GridRow) в сетке (Grid)
                    int rowCount = 0;
                    // Для каждого дополнительного параметра добавляем две строки 
                    // в сетке (для Label и TextBox с Button) и сами элементы: Label и TextBox с Button
                    foreach (string parameter in additionalParameters)
                    {
                        // Добавляем две строки в сетку
                        grid.RowDefinitions.Add(new RowDefinition());
                        grid.RowDefinitions.Add(new RowDefinition());
                        // Создаём экземпляр Label
                        Label label = new Label()
                        {
                            Name = "Label_" + parameter,
                            Content = parameter,
                            VerticalAlignment = VerticalAlignment.Top,
                            HorizontalAlignment = HorizontalAlignment.Left
                        };
                        TextBox tBParam = new TextBox()
                        {
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                            Height = 20,
                            Tag = parameter
                        };

                        Button buttonBrowse = new Button()
                        {
                            Content = "Browse",
                            Tag = parameter,
                            HorizontalAlignment = HorizontalAlignment.Stretch,
                            VerticalAlignment = VerticalAlignment.Center,
                            Height = 20
                        };
                        buttonBrowse.Click += Button_Parameter_Browse_Click;

                        Grid.SetColumn(label, 0);
                        Grid.SetColumn(tBParam, 0);
                        Grid.SetColumn(buttonBrowse, 1);
                        // Устанавливаем для Label и TextBox с Button индексы добавленных строк
                        // (т.е. в каких строках в Grid элементы находятся)
                        // (прямо при добавлении сразу инкрементируем счётчик)
                        Grid.SetRow(label, rowCount++);
                        Grid.SetRow(tBParam, rowCount);
                        Grid.SetRow(buttonBrowse, rowCount++);

                        grid.Children.Add(label);
                        grid.Children.Add(tBParam);
                        grid.Children.Add(buttonBrowse);
                    }
                    gB.Content = grid;

                    StackPanel_AdditionalParameters.Children.Add(gB);
                }
                else if (currentParameter.GetDataRow["AdditionalParametersType"].ToString() == "Options")
                {
                    // Создаём определения строк и столбцов для сетки в GroupBox
                    RowDefinition rowDataGrid = new RowDefinition();
                    RowDefinition rowButtonAdd = new RowDefinition();
                    RowDefinition rowButtonRemove = new RowDefinition();
                    grid.RowDefinitions.Add(rowDataGrid);
                    grid.RowDefinitions.Add(rowButtonAdd);
                    grid.RowDefinitions.Add(rowButtonRemove);
                    // Создаём экземпляр DataGrid, в который будем вводить дополнительные параметры
                    DataGrid dataGrid = new DataGrid()
                    {
                        Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                        VerticalAlignment = VerticalAlignment.Top,
                        HorizontalAlignment = HorizontalAlignment.Stretch,
                        GridLinesVisibility = DataGridGridLinesVisibility.All,
                        CanUserAddRows = false,
                        AutoGenerateColumns = false
                    };
                    // Создаём таблицу, в которой будут храниться введённые 
                    // значения дополнительных параметров
                    DataTable table = new DataTable()
                    {
                        TableName = currentParameter.GetDataRow["NameOfTheParameter"].ToString()
                    };

                    // Инициализируем список с доступными группами опций
                    m_AvailableGroups = new List<string>();
                    /*
                    m_AvailableGroups = DataBaseControl.GetGroupsFromOptionsType(
                        currentParameter.GetDataRow["NameOfTheParameter"].ToString().Split(new char[]{' '})[0]);
                    */
                    m_AvailableGroups = DataBaseControl.GetGroupsFromOptionsTypeByTypeKey(
                        currentParameter.GetDataRow["ParameterInput"].ToString());
                    // Создаём столбец для DataGrid, в основе которого будет ComboBox
                    DataGridTemplateColumn dataGridComboBoxTemplateColumnGroup = new DataGridTemplateColumn()
                    {
                        Header = "Group"
                    };
                    FrameworkElementFactory comboFactoryGroup = new FrameworkElementFactory(typeof(ComboBox));
                    comboFactoryGroup.SetValue(ComboBox.NameProperty, "ComboBox_Group");
                    comboFactoryGroup.SetValue(ComboBox.TagProperty, "ComboBox_Group");
                    comboFactoryGroup.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
                    comboFactoryGroup.SetValue(ComboBox.IsEditableProperty, false);
                    comboFactoryGroup.SetValue(ComboBox.ItemsSourceProperty, m_AvailableGroups);
                    DataTemplate comboTemplateGroup = new DataTemplate();
                    comboTemplateGroup.VisualTree = comboFactoryGroup;
                    dataGridComboBoxTemplateColumnGroup.CellTemplate = comboTemplateGroup;
                    dataGridComboBoxTemplateColumnGroup.CellEditingTemplate = comboTemplateGroup;
                    comboFactoryGroup.AddHandler(ComboBox.SelectionChangedEvent,
                        new SelectionChangedEventHandler(DataGridComboBoxColumn_SelectionChanged));

                    // Инициализируем список с доступными ключами выбранной группы
                    m_AvailableKeys = new List<string>();
                    // Инициализируем привязку к столбцу "Key" таблицы (dataTable)
                    Binding bindKey = new Binding("Key");
                    bindKey.Mode = BindingMode.TwoWay;
                    // Создаём TextBlock
                    FrameworkElementFactory textFactoryKey = new FrameworkElementFactory(typeof(TextBlock));
                    textFactoryKey.SetBinding(TextBlock.TextProperty, bindKey);
                    DataTemplate textTemplateKey = new DataTemplate();
                    textTemplateKey.VisualTree = textFactoryKey;
                    // Создаём ComboBox
                    DataGridTemplateColumn dataGridComboBoxTemplateColumnKey = new DataGridTemplateColumn()
                    {
                        Header = "Key"
                    };
                    FrameworkElementFactory comboFactoryKey = new FrameworkElementFactory(typeof(ComboBox));
                    comboFactoryKey.SetValue(ComboBox.NameProperty, "ComboBox_Key");
                    comboFactoryKey.SetValue(ComboBox.TagProperty, "ComboBox_Key");
                    comboFactoryKey.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
                    comboFactoryKey.SetValue(ComboBox.IsEditableProperty, true);
                    comboFactoryKey.SetValue(ComboBox.ItemsSourceProperty, m_AvailableKeys);
                    comboFactoryKey.SetBinding(ComboBox.TextProperty, bindKey);
                    DataTemplate comboTemplateKey = new DataTemplate();
                    comboTemplateKey.VisualTree = comboFactoryKey;
                    // В качестве исходного состояния ячейки выбираем TextBlock
                    dataGridComboBoxTemplateColumnKey.CellTemplate = textTemplateKey;
                    // В качестве состояния редактирования выбираем ComboBox
                    dataGridComboBoxTemplateColumnKey.CellEditingTemplate = comboTemplateKey;
                    // Добавляем обработчик события "Смена выделенного объекта"
                    comboFactoryKey.AddHandler(ComboBox.SelectionChangedEvent,
                        new SelectionChangedEventHandler(DataGridComboBoxColumn_SelectionChanged));

                    // Инициализируем список с доступными значениями выбранного ключа
                    m_AvailableValues = new List<string>();
                    // Инициализируем привязку к столбцу "Val" таблицы (dataTable)
                    Binding bindVal = new Binding("Val");
                    bindVal.Mode = BindingMode.TwoWay;
                    // Создаём TextBlock
                    FrameworkElementFactory textFactoryVal = new FrameworkElementFactory(typeof(TextBlock));
                    textFactoryVal.SetBinding(TextBlock.TextProperty, bindVal);
                    DataTemplate textTemplateVal = new DataTemplate();
                    textTemplateVal.VisualTree = textFactoryVal;
                    // Создаём ComboBox
                    DataGridTemplateColumn dataGridComboBoxTemplateColumnValue = new DataGridTemplateColumn()
                    {
                        Header = "Val"
                    };
                    FrameworkElementFactory comboFactoryValue = new FrameworkElementFactory(typeof(ComboBox));
                    comboFactoryValue.SetValue(ComboBox.NameProperty, "ComboBox_Value");
                    comboFactoryValue.SetValue(ComboBox.TagProperty, "ComboBox_Value");
                    comboFactoryValue.SetValue(ComboBox.IsTextSearchEnabledProperty, true);
                    comboFactoryValue.SetValue(ComboBox.IsEditableProperty, true);
                    comboFactoryValue.SetValue(ComboBox.ItemsSourceProperty, m_AvailableValues);
                    comboFactoryValue.SetBinding(ComboBox.TextProperty, bindVal);
                    DataTemplate comboTemplateValue = new DataTemplate();
                    comboTemplateValue.VisualTree = comboFactoryValue;
                    // В качестве исходного состояния ячейки выбираем TextBlock
                    dataGridComboBoxTemplateColumnValue.CellTemplate = textTemplateVal;
                    // В качестве состояния редактирования выбираем ComboBox
                    dataGridComboBoxTemplateColumnValue.CellEditingTemplate = comboTemplateValue;

                    // Добавляем созданные столбцы в dataGrid
                    dataGrid.Columns.Add(dataGridComboBoxTemplateColumnGroup);
                    dataGrid.Columns.Add(dataGridComboBoxTemplateColumnKey);
                    dataGrid.Columns.Add(dataGridComboBoxTemplateColumnValue);

                    DataColumn dataTableColumnKey = new DataColumn
                    {
                        ColumnName = "Key",
                        DataType = typeof(String),
                        //Caption = additionalParameters[0]
                        Caption = "Key"
                    };
                    DataColumn dataTableColumnVal = new DataColumn
                    {
                        ColumnName = "Val",
                        DataType = typeof(String),
                        //Caption = additionalParameters[1]
                        Caption = "Val"
                    };
                    table.Columns.Add(dataTableColumnKey);
                    table.Columns.Add(dataTableColumnVal);

                    // Добавляем созданную таблицу в список таблиц для доп. параметров
                    // и назначаем экземпляру DataGrid в качестве источника эту таблицу
                    // из списка
                    m_AdditionalParametersInputs.Add(table);
                    dataGrid.ItemsSource = m_AdditionalParametersInputs.Last<DataTable>().DefaultView;

                    // Создаём кнопку, при нажатии на которую будет добавляться новая строка
                    // в DataGrid
                    Button buttonAddRow = new Button()
                    {
                        Content = "Add new row",
                        Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    // Добавляем этой кнопке обработчик события
                    buttonAddRow.Click += new RoutedEventHandler(Button_AddRow_Click);
                    // Создаём кнопку, при нажатии на которую будет удаляться 
                    // выбранная строка в DataGrid
                    Button buttonRemoveRow = new Button()
                    {
                        Content = "Remove selected row",
                        Tag = currentParameter.GetDataRow["NameOfTheParameter"].ToString(),
                        HorizontalAlignment = HorizontalAlignment.Right,
                        VerticalAlignment = VerticalAlignment.Bottom
                    };
                    // Добавляем этой кнопке обработчик события
                    buttonRemoveRow.Click += new RoutedEventHandler(Button_RemoveRow_Click);
                    // Формируем GroupBox и добавляем его в StackPanel
                    Grid.SetRow(dataGrid, 0);
                    Grid.SetRow(buttonAddRow, 1);
                    Grid.SetRow(buttonRemoveRow, 2);
                    grid.Children.Add(dataGrid);
                    grid.Children.Add(buttonAddRow);
                    grid.Children.Add(buttonRemoveRow);
                    gB.Content = grid;
                    StackPanel_AdditionalParameters.Children.Add(gB);
                }
            }
        }

        private void ParameterRemoved(SelectionChangedEventArgs e)
        {
            // Получаем элемент, с которого сняли выделение
            MyDataRow currentParameter = e.RemovedItems[0] as MyDataRow;
            if (currentParameter == null)
            {
                return;
            }
            // Если параметр вызывается несколько раз, то удаляем его таблицу с введёнными значениями
            if ((bool) currentParameter.GetDataRow["MultipleCalls"] == true | 
                currentParameter.GetDataRow["AdditionalParametersType"].ToString() == "Options")
            {
                try
                {
                    m_AdditionalParametersInputs.Remove(
                        m_AdditionalParametersInputs.First(
                            x => x.TableName == currentParameter.GetDataRow["NameOfTheParameter"])
                        );
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не удалось удалить таблицу с введёнными значениями параметра " +
                        currentParameter.GetDataRow["NameOfTheParameter"]);
                }
            }

            // В StackPanel, где хранятся элементы ввода дополнительных параметров,
            // ищем и удаляем GroupBox, в котором хранятся доп. параметры
            // текущего параметра
            foreach (var child in StackPanel_AdditionalParameters.Children)
            {
                if (child != null && child is GroupBox)
                {
                    GroupBox gB = child as GroupBox;
                    if (gB.Tag == currentParameter.GetDataRow["NameOfTheParameter"])
                    {
                        StackPanel_AdditionalParameters.Children.Remove(gB);
                        return;
                    }
                }
            }
            // Если этот параметр можно вызывать несколько раз, то заодно
            // удаляем DataTable для этого параметра из списка
            if ((bool)currentParameter.GetDataRow["MultipleCalls"] == true)
            {
                m_AdditionalParametersInputs.RemoveAll(
                    x => x.TableName == currentParameter.GetDataRow["NameOfTheParameter"].ToString());
            }
        }

        private void ParametersArgumentForming()
        {
            // Формируем массив, в котором будут храниться добавленные
            // параметры в том порядке, как указано в самой утилите
            m_AllParameters = new string[(int)m_UtilityParameters.Max(x => x.GetDataRow["PositionIndex"]) + 1]; 

            // Инициируем переменную, в которой будет храниться индекс 
            // положения параметра
            int positionIndex = 0;

            // Просматриваем все выделенные в списке параметры
            foreach (MyDataRow parameter in ListBox_AvailableParameters.SelectedItems)
            {
                // Получаем индекс положения параметра
                positionIndex = (int) parameter.GetDataRow["PositionIndex"];

                // Инициируем переменную для шаблона
                string pattern = String.Empty;
                // Проверяем, есть ли для данного параметра несколько шаблонов 
                // (из разных версий утилит)
                // Если более одного шаблона, то получаем все шаблоны, проверяем их версии,
                // и выбираем наиболее "свежий" шаблон для данной версии утилиты
                if (parameter.GetDataRow["Pattern"].ToString().Split(';').Length > 1)
                {
                    try
                    {
                        string[] differentVersionsPatterns =
                            parameter.GetDataRow["Pattern"].ToString().Split(
                                new char[] {';'},
                                StringSplitOptions.RemoveEmptyEntries);
                        Version maxVersion =
                            Version.Parse(differentVersionsPatterns[0].Split(
                                new char[] {'|'},
                                StringSplitOptions.RemoveEmptyEntries)[0]);
                        for (int i = 1; i < differentVersionsPatterns.Length; i++)
                        {
                            Version currentVer =
                                Version.Parse(differentVersionsPatterns[i].Split(
                                    new char[] {'|'},
                                    StringSplitOptions.RemoveEmptyEntries)[0]);
                            if (currentVer <= m_UtilityVersion && currentVer > maxVersion)
                            {
                                maxVersion = currentVer;
                            }
                        }
                        pattern =
                            differentVersionsPatterns.Where(x => x.Contains(maxVersion.ToString()) == true)
                                .First()
                                .Split(
                                    new Char[] {'|'},
                                    StringSplitOptions.RemoveEmptyEntries)[1];
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show("Не удалось получить шаблон параметра. Вероятно проблема в базе данных. " +
                                        Environment.NewLine + 
                                        "Параметр " + parameter.ToString() + " будет пропущен."
                            + Environment.NewLine + ex.Data.ToString());
                        m_AllParameters[positionIndex] = String.Empty;
                        continue;
                    }
                }
                // Если только один шаблон, то его и добавляем
                else
                {
                    if (String.IsNullOrEmpty(parameter.GetDataRow["Pattern"].ToString()))
                    {
                        MessageBox.Show("В базе данных отсутствует шаблон." + Environment.NewLine +
                                        "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        m_AllParameters[positionIndex] = String.Empty;
                        continue;
                    }
                    pattern = parameter.GetDataRow["Pattern"].ToString();
                }

                // Проверяем, есть ли у параметра дополнительные параметры
                // Если есть, то начинаем получение введённых дополнительных параметров
                if ((bool) parameter.GetDataRow["IsThereAdditionalParameters"] == true)
                {
                    // Инициируем переменную для элемента GroupBox, в котором
                    // хранятся поля ввода дополнительных параметров, и получаем его
                    // для соответствующего параметра
                    GroupBox gB= null;
                    foreach (var child in StackPanel_AdditionalParameters.Children)
                    {
                        if (child is GroupBox && (child as GroupBox).Tag == parameter.GetDataRow["NameOfTheParameter"])
                        {
                            gB = child as GroupBox;
                        }
                    }

                    if (gB == null)
                    {
                        MessageBox.Show("Не удалось получить дополнительные параметры." + Environment.NewLine +
                                        "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        m_AllParameters[positionIndex] = String.Empty;
                        continue;
                    }

                    if (String.IsNullOrEmpty(parameter.GetDataRow["AdditionalParametersType"].ToString()))
                    {
                        MessageBox.Show("В базе отсутствует тип дополнительных параметров (AdditionalParametersType)." 
                                        + Environment.NewLine + "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                        m_AllParameters[positionIndex] = String.Empty;
                        continue;
                    }
                    // Проверяем, должен ли пользователь вводить эти параметры  вручную.
                    if (parameter.GetDataRow["AdditionalParametersType"].ToString() == "ManualInput")
                    {
                        // Проверяем, можно ли этот параметр вызвать несколько раз
                        if ((bool) parameter.GetDataRow["MultipleCalls"] == true)
                        {
                            // Получаем таблицу, в которой хранятся введённые доп. параметры
                            DataTable tableWithInputedParameters =
                                m_AdditionalParametersInputs.Where(x => x.TableName == parameter.GetDataRow["NameOfTheParameter"].ToString()).First();
                            // Формируем шаблон регулярных выражений, чтобы корректно 
                            // вставлять данные 
                            // (чтобы корректно отличать обычные параметры, вроде src_min от 
                            // таких как _bn, где важно сохранить символ "_") 
                            Regex regex = new Regex("([0-9a-zA-Z]+_[0-9a-zA-Z]+|[0-9a-zA-Z]+)");

                            // Просматриваем все записи в таблице (введённые доп.параметры
                            // для каждого отдельного вызова данного параметра
                            foreach (DataRow row in tableWithInputedParameters.Rows)
                            {
                                // Каждый вызов параметры отделяем пробелом
                                m_AllParameters[positionIndex] += pattern + " ";
                                // Проходим по всем столбцам (доп. параметрам)
                                // Если что-то введено, то добавляется значение в шаблон
                                // Если ничего не введено, то параметр удаляется из шаблона
                                for (int i = 0; i < row.ItemArray.Length; i++)
                                {
                                    string columnName = tableWithInputedParameters.Columns[i].ColumnName;
                                    if (!String.IsNullOrEmpty(row[i].ToString()))
                                    {
                                        String match = regex.Match(columnName).ToString();
                                        m_AllParameters[positionIndex] =
                                            m_AllParameters[positionIndex].Replace(match, row[i].ToString());
                                    }
                                    else
                                    {
                                        m_AllParameters[positionIndex] =
                                            m_AllParameters[positionIndex].Replace(columnName, String.Empty);
                                    }
                                }
                            }
                        }
                        // Если этот параметр может вызываться только единожды
                        else if((bool)parameter.GetDataRow["MultipleCalls"] == false) 
                        {
                            try
                            {
                                m_AllParameters[positionIndex] = pattern;
                                // Получаем сетку с полями ввода
                                Grid grid = gB.Content as Grid;
                                // Проходимся по всем полям ввода, получаем значения и добавляем в шаблон
                                foreach (var child in grid.Children)
                                {
                                    if (child is TextBox)
                                    {
                                        TextBox tB = child as TextBox;
                                        if (!String.IsNullOrEmpty(tB.Text))
                                        {
                                            m_AllParameters[positionIndex] =
                                                m_AllParameters[positionIndex].Replace(tB.Tag.ToString(), tB.Text);
                                        }
                                        else
                                        {
                                            m_AllParameters[positionIndex] =
                                                m_AllParameters[positionIndex].Replace(tB.Tag.ToString(), String.Empty);
                                        }
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                MessageBox.Show("Не удалось добавить введённые значения в шаблон." 
                                        + Environment.NewLine + "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                                m_AllParameters[positionIndex] = String.Empty;
                                continue;
                            }
                        }
                    }
                    // Если параметр требует от пользователя выбрать один из вариантов
                    else if (parameter.GetDataRow["AdditionalParametersType"].ToString() == "Selecting")
                    {
                        try
                        {
                            // Получаем ComboBox, в котором пользователь выбирал доп. параметр
                            ComboBox cB = gB.Content as ComboBox;
                            //allParameters[positionIndex] = parameter.GetDataRow["Pattern"].ToString();
                            // Добавляем шаблон и добавляем в него значение
                            m_AllParameters[positionIndex] = pattern;
                            m_AllParameters[positionIndex] =
                                m_AllParameters[positionIndex].Replace("selected_value", cB.SelectedItem.ToString());
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Не удалось добавить выбранное значение в шаблон."
                                        + Environment.NewLine + "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                            m_AllParameters[positionIndex] = String.Empty;
                            continue;
                        }
                    }
                    else if (parameter.GetDataRow["AdditionalParametersType"].ToString() == "OpenFile")
                    {
                        try
                        {
                            // Получаем Grid, в котором хранится TextBox с введёнными значениями
                            Grid grid = gB.Content as Grid;
                            m_AllParameters[positionIndex] = pattern;
                            // Ищем TextBox'ы, добавляем значения в шаблон
                            foreach (var child in grid.Children)
                            {
                                if (child is TextBox)
                                {
                                    TextBox tB = child as TextBox;
                                    if (!String.IsNullOrEmpty(tB.Text))
                                    {
                                        m_AllParameters[positionIndex] = 
                                            m_AllParameters[positionIndex].Replace(tB.Tag.ToString(), "\"" + tB.Text + "\"") ;
                                    }
                                    else
                                    {
                                        m_AllParameters[positionIndex] =
                                            m_AllParameters[positionIndex].Replace(tB.Tag.ToString(), String.Empty);
                                    }
                                }
                            }

                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show("Не удалось добавить выбранное значение в шаблон."
                                        + Environment.NewLine + "Параметр " + parameter.ToString() + " будет пропущен.",
                                        "Ошибка!",
                                        MessageBoxButton.OK, MessageBoxImage.Error);
                            m_AllParameters[positionIndex] = String.Empty;
                            continue;
                        }
                    }
                    else if (parameter.GetDataRow["AdditionalParametersType"].ToString() == "Options")
                    {
                        // Получаем таблицу, в которой хранятся введённые доп. параметры
                        DataTable tableWithInputedParameters =
                            m_AdditionalParametersInputs.Where(x => x.TableName == parameter.GetDataRow["NameOfTheParameter"].ToString()).First();
                        // Формируем шаблон регулярных выражений, чтобы корректно 
                        // вставлять данные 
                        // (чтобы корректно отличать обычные параметры, вроде src_min от 
                        // таких как _bn, где важно сохранить символ "_") 
                        Regex regex = new Regex("([0-9a-zA-Z]+_[0-9a-zA-Z]+|[0-9a-zA-Z]+)");

                        // Просматриваем все записи в таблице (введённые доп.параметры
                        // для каждого отдельного вызова данного параметра
                        foreach (DataRow row in tableWithInputedParameters.Rows)
                        {
                            // Каждый вызов параметры отделяем пробелом
                            m_AllParameters[positionIndex] += pattern + " ";
                            // Проходим по всем столбцам (доп. параметрам)
                            // Если что-то введено, то добавляется значение в шаблон
                            // Если ничего не введено, то параметр удаляется из шаблона
                            for (int i = 0; i < row.ItemArray.Length; i++)
                            {
                                string columnName = tableWithInputedParameters.Columns[i].ColumnName;
                                if (!String.IsNullOrEmpty(row[i].ToString()))
                                {
                                    String match = regex.Match(columnName).ToString();
                                    m_AllParameters[positionIndex] =
                                        m_AllParameters[positionIndex].Replace(match, row[i].ToString());
                                }
                                else
                                {
                                    m_AllParameters[positionIndex] =
                                        m_AllParameters[positionIndex].Replace(columnName, String.Empty);
                                }
                            }
                        }
                    }
                }
                // Если нет дополнительных параметров, то в массив выбранных параметров
                // добавляем шаблон
                else
                {
                    //allParameters[positionIndex] = parameter.GetDataRow["Pattern"].ToString();
                    m_AllParameters[positionIndex] = pattern;
                }
            }
        }

        private void InputAndOutputToParametersArgumentString(int index)
        {
            // Если утилита поддерживает входные данные 
            if ((bool) m_UtilityInfo["IsThereInput"] == true && !String.IsNullOrEmpty( m_InputFiles[index]))
            {
                // Получаем индекс параметра входных данных
                int src_Position =
                (int)m_UtilityParameters.Where(
                    x => x.GetDataRow["NameOfTheParameter"].ToString() == "src_dataset").First().GetDataRow["PositionIndex"];

                string buffer = String.Empty;
                if (m_CurrentMode == InputMode.MultipleFilesForOneTask ||
                    m_CurrentMode == InputMode.FromAnotherTasksForOneTask)
                {
                    foreach (string name in m_InputFiles)
                    {
                        buffer += "\"" + name + "\" ";
                    }
                    m_AllParameters[src_Position] = buffer;
                }
                else
                {
                    m_AllParameters[src_Position] = "\"" + m_InputFiles[index] + "\"";
                }
            }
            // Если утилита поддерживает выходные данные 
            //if ((bool)m_UtilityInfo["IsThereOutput"] == true && !String.IsNullOrEmpty(m_OutputPath))
            if ((bool)m_UtilityInfo["IsThereOutput"] == true)
            {
                // Получаем индекс параметра выходных данных
                int dst_Position =
                (int)m_UtilityParameters.Where(
                    x => x.GetDataRow["NameOfTheParameter"].ToString() == "dst_dataset").First().GetDataRow["PositionIndex"];
                // Добавляем путь
                if (m_CurrentMode == InputMode.OneFile || 
                    m_CurrentMode == InputMode.FromAnotherTasksForOneTask || 
                    m_CurrentMode == InputMode.MultipleFilesForOneTask)
                {
                    //m_AllParameters[dst_Position] = m_OutputPath;
                    m_AllParameters[dst_Position] = "\"" + m_OutputPath + "\"";
                }
                else if (m_CurrentMode == InputMode.MultipleFilesForMultipleTasks ||
                         m_CurrentMode == InputMode.FromAnotherTasksForMultipleTasks)
                {
                    m_AllParameters[dst_Position] = "\"" + 
                        System.IO.Path.Combine(m_OutputPath,
                        System.IO.Path.GetFileNameWithoutExtension(m_InputFiles[index]) +
                        "_Edited_Task_" + m_Task.GetTaskID + System.IO.Path.GetExtension(m_InputFiles[index]))
                        + "\"";
                }
            }
        }

        private void CompleteParametersArgumentString()
        {
            m_FormedParametersArgument = String.Empty;
            // Формируем единую строку параметров
            foreach (string filledParameter in m_AllParameters)
            {
                m_FormedParametersArgument += filledParameter + " ";
            }
            Regex regex = new Regex(@"\s{2,}");
            MatchCollection matches = regex.Matches(m_FormedParametersArgument);
            foreach (Match match in matches)
            {
                m_FormedParametersArgument = m_FormedParametersArgument.Replace(match.ToString(), " ");
            }
        }

        private void MakeTask(int index)
        {
            if (m_TaskEditWindowMode == TaskEditWindowMode.NewTask)
            {
                m_Task = new MyTask(m_MainWindow);
            }
            m_Task.BeginEdit();
            m_Task.ParametersString = m_FormedParametersArgument;
            m_Task.UtilityName = ComboBox_UtilitiesNames.SelectedItem.ToString();
            m_Task.SrcFileName = m_InputFiles[index];
            if (m_ThumbnailsPaths != null)
            {
                m_Task.ThumbnailPath = m_ThumbnailsPaths[index];
            }
            else
            {
                m_Task.ThumbnailPath = String.Empty;
            }
            m_Task.IsThereOutput = (bool)m_UtilityInfo["IsThereOutput"];
            if (m_Task.IsThereOutput == true)
            {
                int dst_Position =
                    (int)m_UtilityParameters.First(
                        x => x.GetDataRow["NameOfTheParameter"].ToString() == "dst_dataset").GetDataRow["PositionIndex"];
                m_Task.OutputPath = m_AllParameters[dst_Position];
            }
            else
            {
                m_Task.OutputPath = String.Empty;
            }
            m_Task.EndEdit();
        }

        private void MakeThumbnails()
        {
            m_ThumbnailsPaths = new string[m_InputFiles.Count];
            for (int i = 0; i < m_InputFiles.Count; i++)
            {
                //m_ThumbnailsPaths[i] = System.IO.Path.GetTempFileName();
                while (true)
                {
                    m_ThumbnailsPaths[i] = System.IO.Path.Combine(Properties.Settings.Default.TempDirectory,
                        System.IO.Path.GetRandomFileName());
                    if (!System.IO.File.Exists(m_ThumbnailsPaths[i]))
                    {
                        break;
                    }
                }
                GdalFunctions.MakeThumbnail(m_InputFiles[i], ref m_ThumbnailsPaths[i]);
            }
            Image_Preview.BeginInit();
            if (m_ThumbnailsPaths[0]!= null && System.IO.File.Exists(m_ThumbnailsPaths[0]))
            {
                try
                {
                    //Image_Preview.Source = new BitmapImage(new Uri(m_ThumbnailsPaths[0]));
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                    image.UriSource = new Uri(m_ThumbnailsPaths[0]);
                    image.EndInit();
                    Image_Preview.Source = image;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Не удалось установить миниатюру: " + m_ThumbnailsPaths[0]);
                    Image_Preview.Source = 
                        new BitmapImage(new Uri(Properties.Settings.Default.ImageNotAvailableRelativePath, UriKind.Relative));
                }
            }
            else
            {
                MessageBox.Show(
                    "Не удалось отобразить миниатуюру.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                Image_Preview.Source =
                    new BitmapImage(new Uri(Properties.Settings.Default.ImageNotAvailableRelativePath, UriKind.Relative));

            }
            Image_Preview.EndInit();
        }

        private void MoveFileInListBox(int direction)
        {
            // Checking selected item
            if (ListBox_SelectedFiles.SelectedItem == null || ListBox_SelectedFiles.SelectedIndex < 0)
                return; // No selected item - nothing to do

            if (!m_InputFiles.Contains(ListBox_SelectedFiles.SelectedItem))
                return;

            // Calculate new index using move direction
            int newIndex = ListBox_SelectedFiles.SelectedIndex + direction;

            // Checking bounds of the range
            if (newIndex < 0 || newIndex >= ListBox_SelectedFiles.Items.Count)
                return; // Index out of range - nothing to do

            object selected = ListBox_SelectedFiles.SelectedItem;

            // Removing removable element
            //ListBox_SelectedFiles.Items.Remove(selected);
            m_InputFiles.Remove(selected.ToString());
            // Insert it in new position
            //ListBox_SelectedFiles.Items.Insert(newIndex, selected);
            m_InputFiles.Insert(newIndex, selected.ToString());
            ListBox_SelectedFiles.Items.Refresh();
            // Restore selection
            //ListBox_SelectedFiles.SelectedItem = ListBox_SelectedFiles.Items[newIndex];
            ListBox_SelectedFiles.SelectedItem = m_InputFiles[newIndex];
        }

        private void RestorePreviousStateOfTask()
        {
            // Восстановление предыдущего состояния окна
            m_ThumbnailsPaths = new string[1];
            ComboBox_UtilitiesNames.SelectedItem = m_Task.UtilityName;
            m_UtilityParameters = m_Task.ParametersList;
            ListBox_AvailableParameters.ItemsSource =
                m_UtilityParameters.Where(x => (bool)x.GetDataRow["MustBeInAvailableParametersList"] == true);
            foreach (MyDataRow selectedParam in m_Task.SelectedParametersList)
            {
                if (m_UtilityParameters.Contains(selectedParam))
                {
                    ListBox_AvailableParameters.SelectedItems.Add(selectedParam);
                }
            }
            StackPanel_AdditionalParameters.Children.Clear();
            foreach (GroupBox gB in m_Task.AdditionalParameters)
            {
                StackPanel_AdditionalParameters.Children.Add(gB);
            }
            m_AdditionalParametersInputs = m_Task.AdditionalParametersInputs;
            m_InputFiles = m_Task.InputFilesList;
            foreach (string file in m_InputFiles)
            {
                TextBox_InputFile.Text += file + " ";
            }
            m_OutputPath = m_Task.OutputPath.Replace("\"", "");
            TextBox_OutputFile.Text = m_OutputPath;
            this.SetResourceReference(Window.TitleProperty, "m_TaskEditWindow_Title_EditMode");
            (this.menu.Items[0] as System.Windows.Controls.MenuItem).
                SetResourceReference(System.Windows.Controls.MenuItem.HeaderProperty, "m_TaskEdit_Menu_AddTask_EditMode");
            this.Title += m_Task.GetTaskID;
            m_ThumbnailsPaths[0] = m_Task.ThumbnailPath;
            if (!String.IsNullOrEmpty(m_ThumbnailsPaths[0]))
            {
                try
                {
                    //Image_Preview.Source = new BitmapImage(new Uri(m_ThumbnailsPaths[0], UriKind.RelativeOrAbsolute));
                    BitmapImage image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.UriSource = new Uri(m_ThumbnailsPaths[0]);
                    image.EndInit();
                    Image_Preview.Source = image;

                }
                catch (Exception ex)
                {
                    MessageBox.Show("Не удалось загрузить миниатюру", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    Image_Preview.Source =
                        new BitmapImage(new Uri(Properties.Settings.Default.ImageNotAvailableRelativePath,
                            UriKind.Relative)); ;
                }
            }
            else
            {
                MessageBox.Show("Отсутствует миниатуюра.", "Информация",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                Image_Preview.Source =
                    new BitmapImage(new Uri(Properties.Settings.Default.ImageNotAvailableRelativePath,
                        UriKind.Relative));
            }
        }

        #endregion

        // Обработчики событий
        #region Обработчики событий

        private void ThisWindow_Closing(object sender, CancelEventArgs e)
        {
            if (m_IsThisTaskAdded == true)
            {
                DataBaseControl.CloseConnection();
                return;
            }
            MessageBoxResult result = MessageBox.Show("Изменения не будут сохранены. Вы уверены, что хотите выйти?",
                "Внимание!", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
            {
                DataBaseControl.CloseConnection();
                StackPanel_AdditionalParameters.Children.Clear();
                if (m_ThumbnailsPaths != null && m_ThumbnailsPaths.Length > 0)
                {
                    for (int i = 0; i < m_ThumbnailsPaths.Length; i++)
                    {
                        try
                        {
                            File.Delete(m_ThumbnailsPaths[i]);
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Не удалось удалить временный файл: " + m_ThumbnailsPaths[i] + 
                                Environment.NewLine + ex.Message);
                        }
                    }
                }
                return;
            }
            else
            {
                e.Cancel = true;
            }
        }

        private void Button_BrowseInputFile_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = m_InputFilter;
            SelectTaskDialogWindow selectTaskDialogWindow = new SelectTaskDialogWindow(m_MainWindow.GetTasksList);
            switch (m_CurrentMode)
            {
                case InputMode.OneFile:
                    openFileDialog. Multiselect = false;
                    openFileDialog.CheckPathExists = true;
                    if (openFileDialog.ShowDialog() == true)
                    {
                        m_InputFiles.AddRange(openFileDialog.FileNames);
                        ListBox_SelectedFiles.Items.Clear();
                        ListBox_SelectedFiles.ItemsSource = m_InputFiles;
                        /*
                        foreach (string fileName in m_InputFiles)
                        {
                            ListBox_SelectedFiles.Items.Add(fileName);
                        }
                        */
                        if (Properties.Settings.Default.GenerateThumbnails == true)
                        {
                            MakeThumbnails();
                        }
                        TextBox_InputFile.Text = m_InputFiles[0];
                    }
                    break;
                case InputMode.MultipleFilesForOneTask:
                    openFileDialog.Multiselect = true;
                    openFileDialog.CheckPathExists = true;
                    if (openFileDialog.ShowDialog() == true)
                    {
                        m_InputFiles.AddRange(openFileDialog.FileNames);
                        ListBox_SelectedFiles.Items.Clear();
                        ListBox_SelectedFiles.ItemsSource = m_InputFiles;
                        /*
                        foreach (string fileName in m_InputFiles)
                        {
                            ListBox_SelectedFiles.Items.Add(fileName);
                        }
                        */
                        if (Properties.Settings.Default.GenerateThumbnails == true)
                        {
                            MakeThumbnails();
                        }
                        foreach (string file in m_InputFiles)
                        {
                            TextBox_InputFile.Text += file + "; ";
                        }
                    }
                    break;
                case InputMode.MultipleFilesForMultipleTasks:
                    openFileDialog.Multiselect = true;
                    openFileDialog.CheckPathExists = true;
                    if (openFileDialog.ShowDialog() == true)
                    {
                        m_InputFiles.AddRange(openFileDialog.FileNames);
                        ListBox_SelectedFiles.Items.Clear();
                        ListBox_SelectedFiles.ItemsSource = m_InputFiles;
                        /*
                        foreach (string fileName in m_InputFiles)
                        {
                            ListBox_SelectedFiles.Items.Add(fileName);
                        }
                        */
                        if (Properties.Settings.Default.GenerateThumbnails == true)
                        {
                            MakeThumbnails();
                        }
                        foreach (string file in m_InputFiles)
                        {
                            TextBox_InputFile.Text += file + "; ";
                        }
                    }
                    break;
                case InputMode.FromAnotherTasksForOneTask:
                    if (selectTaskDialogWindow.ShowDialog() == true)
                    {
                        m_InputFiles.AddRange(selectTaskDialogWindow.FileNames);
                        ListBox_SelectedFiles.Items.Clear();
                        ListBox_SelectedFiles.ItemsSource = m_InputFiles;
                        /*
                        foreach (string fileName in m_InputFiles)
                        {
                            ListBox_SelectedFiles.Items.Add(fileName);
                        }
                        */
                    }
                    break;
                case InputMode.FromAnotherTasksForMultipleTasks:
                    if (selectTaskDialogWindow.ShowDialog() == true)
                    {
                        m_InputFiles.AddRange(selectTaskDialogWindow.FileNames);
                        ListBox_SelectedFiles.Items.Clear();
                        ListBox_SelectedFiles.ItemsSource = m_InputFiles;
                        /*
                        foreach (string fileName in m_InputFiles)
                        {
                            ListBox_SelectedFiles.Items.Add(fileName);
                        }
                        */
                    }
                    break;
            }
        }

        private void Button_BrowseOutputFile_Click(object sender, RoutedEventArgs e)
        {
            switch (m_CurrentMode)
            {
                case InputMode.OneFile:
                    SaveFileDialog saveFileDialog_OneInput = new SaveFileDialog();
                    saveFileDialog_OneInput.Filter = m_OutputFilter;
                    if (saveFileDialog_OneInput.ShowDialog() == true)
                    {
                        OutputFilePath = saveFileDialog_OneInput.FileName;
                    }
                    break;
                case InputMode.MultipleFilesForOneTask:
                    SaveFileDialog saveFileDialog_MultipleInput = new SaveFileDialog();
                    saveFileDialog_MultipleInput.Filter = m_OutputFilter;
                    if (saveFileDialog_MultipleInput.ShowDialog() == true)
                    {
                        OutputFilePath = saveFileDialog_MultipleInput.FileName;
                    }
                    break;
                case InputMode.MultipleFilesForMultipleTasks:
                    System.Windows.Forms.FolderBrowserDialog folderBrowserDialog =
                        new System.Windows.Forms.FolderBrowserDialog();
                    if (folderBrowserDialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        OutputFilePath = folderBrowserDialog.SelectedPath;
                    }
                    break;
                case InputMode.FromAnotherTasksForOneTask:
                    SaveFileDialog saveFileDialog_FromAnotherUtility = new SaveFileDialog();
                    saveFileDialog_FromAnotherUtility.Filter = m_OutputFilter;
                    if (saveFileDialog_FromAnotherUtility.ShowDialog() == true)
                    {
                        OutputFilePath = saveFileDialog_FromAnotherUtility.FileName;
                    }
                    break;
                case InputMode.FromAnotherTasksForMultipleTasks:
                    System.Windows.Forms.FolderBrowserDialog folderBrowserDialog_FromAnotherTasks =
                        new System.Windows.Forms.FolderBrowserDialog();
                    if (folderBrowserDialog_FromAnotherTasks.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                    {
                        OutputFilePath = folderBrowserDialog_FromAnotherTasks.SelectedPath;
                    }
                    break;
            }

            
        }

        private void TaskEdit_Menu_AddTask_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if ((bool)m_UtilityInfo["IsThereInput"] == true & 
                    (m_InputFiles == null || String.IsNullOrEmpty(m_InputFiles[0])) )
                {
                    MessageBox.Show("Не указан путь до входных данных!", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception("");
                }
                if ((bool)m_UtilityInfo["IsThereOutput"] == true & String.IsNullOrEmpty(m_OutputPath))
                {
                    MessageBox.Show("Не указан путь сохранения результата!", "Ошибка!",
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    throw new Exception("");
                }

                int numOfFormingUtilities = 0;
                if (m_CurrentMode == InputMode.OneFile ||
                    m_CurrentMode == InputMode.FromAnotherTasksForOneTask ||
                    m_CurrentMode == InputMode.MultipleFilesForOneTask)
                {
                    numOfFormingUtilities = 1;
                }
                else
                {
                    numOfFormingUtilities = m_InputFiles.Count;
                }
                
                ParametersArgumentForming();
                
                for (int i = 0; i < numOfFormingUtilities; i++)
                {
                    InputAndOutputToParametersArgumentString(i);
                    CompleteParametersArgumentString();
                    MakeTask(i);

                    // Процесс сохранения всех выбранных параметров и т.п. на случай,
                    // если пользователь захочет отредактировать задачу
                    m_Task.ParametersList = m_UtilityParameters;
                    m_Task.SelectedParametersList = new MyDataRow[ListBox_AvailableParameters.SelectedItems.Count];
                    ListBox_AvailableParameters.SelectedItems.CopyTo(m_Task.SelectedParametersList, 0);
                    m_Task.AdditionalParameters = new GroupBox[StackPanel_AdditionalParameters.Children.Count];
                    StackPanel_AdditionalParameters.Children.CopyTo(m_Task.AdditionalParameters, 0);
                    m_Task.AdditionalParametersInputs = m_AdditionalParametersInputs;
                    m_Task.CurrentInputMode = m_CurrentMode;
                    m_Task.InputFilesList = m_InputFiles;

                    if (m_TaskEditWindowMode == TaskEditWindowMode.NewTask)
                    {
                        m_MainWindow.AddNewTask(m_Task);
                    }
                    else if (m_TaskEditWindowMode == TaskEditWindowMode.EditingExistingTask)
                    {
                        m_MainWindow.ReplaceEditedTask(m_Task);
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("Не удалось завершить процесс добавления задачи." +
                                Environment.NewLine + ex.Message);
                return;
            }
            //finally
            //{
                StackPanel_AdditionalParameters.Children.Clear();
                m_IsThisTaskAdded = true;
                this.Close();
            //}
        }

        private void TaskEdit_Menu_ExitWithoutAdding_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        // При смене утилиты определяется её версия, 
        // очищается список доступных параметров и загружается новый список,
        // а также выводится описание выбранной утилиты
        private void ComboBox_UtilitiesNames_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Получаем имя выбранной утилиты (т.к. выбирается только один объект,
            // то в массиве AddedItems он будет всегда находиться под индексом 0)
            string name = e.AddedItems[0].ToString();

            // Получаем запускаем процесс с параметром --version, 
            // чтобы получить версию выбранной утилиты
            m_ProcessForVersion.StartInfo.FileName = 
                Properties.Settings.Default.UtilitiesDirectory + name;
            m_ProcessForVersion.Start();
            m_ProcessForVersion.BeginOutputReadLine();
            m_ProcessForVersion.WaitForExit();
            m_ProcessForVersion.CancelOutputRead();
            m_ProcessForVersion.Close();
            TextBox_UtilityVersion.Text = m_UtilityVersion.ToString();

            StackPanel_AdditionalParameters.Children.Clear();

            // Получаем параметры для данной утилиты, 
            // проверяем их совместимость с выбранной версией утилиты и
            // выводим список в ListBox
            m_UtilityParameters = DataBaseControl.GetUtilityParameters(name);
            m_UtilityParameters.RemoveAll(x => new Version(x.GetDataRow["Version"].ToString()) > m_UtilityVersion);
            //ListBox_AvailableParameters.ItemsSource = m_UtilityParameters;
            // Добавляем только те параметры, которые должны выводиться 
            // в списке доступных параметров (отсекает src_dataset и dst_dataset)
            ListBox_AvailableParameters.ItemsSource =
                m_UtilityParameters.Where(x => (bool)x.GetDataRow["MustBeInAvailableParametersList"] == true);

            // Выводим описание выбранной утилиты в соответствии с 
            // выбранным в настройках языком
            m_UtilityInfo = DataBaseControl.GetUtilityInfo(e.AddedItems[0].ToString());
            TextBlock_UtilityDescription.Text = 
                m_UtilityInfo["Description"+Properties.Settings.Default.DescriptionsLanguage].ToString();

            // Если утилита не поддерживает вход/выход, то отключаем соответствующие
            // GroupBox, в которых выбираются входные/выходные данные
            GroupBox_InputPaths.IsEnabled = (bool) m_UtilityInfo["IsThereInput"];
            GroupBox_OutputPaths.IsEnabled = (bool)m_UtilityInfo["IsThereOutput"];
        }

        // Меняет режим выбора входного файла
        private void ComboBox_InputOptions_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cB = sender as ComboBox;
            if (cB.SelectedIndex >= 0)
            {
                m_CurrentMode = (InputMode) cB.SelectedValue;
            }
        }

        private void Button_FileList_MoveUp_Click(object sender, RoutedEventArgs e)
        {
            MoveFileInListBox(-1);
        }

        private void Button_FileList_MoveDown_Click(object sender, RoutedEventArgs e)
        {
            MoveFileInListBox(1);
        }

        // При выборе параметра из ListBox проверяется наличие у него дополнительных параметров.
        // Эти параметры либо добавляются в StackPanel, либо убираются оттуда, если с параметра
        // сняли выделение
        private void ListBox_AvailableParameters_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (e.AddedItems != null && e.AddedItems.Count > 0)
            {
                ParameterAdded(e);
            }
            else if (e.RemovedItems != null && e.RemovedItems.Count > 0)
            {
                ParameterRemoved(e);
            }
        }

        // Этот обработчик добавляется к событию элементов в списке (как только мышь попадёт на элемент списка)
        // Выводится описание параметра, на которого наведён курсор
        // Сами события добавляются к элементам в xaml коде через:
        // ItemContainerStyle - Style - EventSetter
        private void ListBox_AvailableParameters_Item_MouseEnter(object sender, MouseEventArgs e)
        {
            ListBoxItem lBI = sender as ListBoxItem;
            TextBlock_ParameterDescription.Text = (lBI.Content as MyDataRow).GetDataRow[
                "ParameterDescription" + Properties.Settings.Default.DescriptionsLanguage].ToString();
        }

        private void Button_AddRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataTable table = m_AdditionalParametersInputs.Find(x => x.TableName == btn.Tag.ToString());
            table.Rows.Add();
        }

        private void Button_RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            DataTable table = m_AdditionalParametersInputs.Find(x => x.TableName == btn.Tag.ToString());

            Grid grid = btn.Parent as Grid;
            foreach (UIElement child in grid.Children)
            {
                if (child.GetType() == typeof(DataGrid) && (child as DataGrid).Tag == btn.Tag)
                {
                    if ((child as DataGrid).SelectedIndex != -1)
                    {
                        table.Rows[(child as DataGrid).SelectedIndex].Delete();
                    }
                    else
                    {
                        MessageBox.Show("Не выбрана строка, которую необходимо удалить!",
                            "Ошибка!", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void Button_Parameter_Browse_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            Grid grid = btn.Parent as Grid;

            OpenFileDialog openFileDialog = new OpenFileDialog();
            if (openFileDialog.ShowDialog() == true)
            {
                foreach (UIElement child in grid.Children)
                {
                    if (child.GetType() == typeof(TextBox) && (child as TextBox).Tag == btn.Tag)
                    {
                        (child as TextBox).Text = "\"" + openFileDialog.FileName + "\"";
                        break;
                    }
                }
            }
        }

        private void DataGridComboBoxColumn_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ComboBox cB = sender as ComboBox;
            switch (cB.Name)
            {
                case "ComboBox_Group":
                    m_AvailableKeysWithValues = new List<DataRow>();
                    if (e.AddedItems != null && e.AddedItems.Count > 0)
                    {
                        m_AvailableKeysWithValues = DataBaseControl.GetKeysWithValuesFromGroup(e.AddedItems[0].ToString());
                        if (m_AvailableKeysWithValues != null)
                        {
                            m_AvailableKeys.Clear();
                            foreach (DataRow row in m_AvailableKeysWithValues)
                            {
                                m_AvailableKeys.Add(row["Key"].ToString());
                            }
                        }
                    }
                    break;
                case "ComboBox_Key":
                    if (e.AddedItems != null && e.AddedItems.Count > 0)
                    {
                        //m_AvailableValues = new List<string>();
                        if (m_AvailableKeysWithValues != null)
                        {
                            m_AvailableValues.Clear();
                            DataRow foundedRow = m_AvailableKeysWithValues.First(x => x["Key"].ToString() == e.AddedItems[0].ToString());
                            m_AvailableValues.AddRange(
                                foundedRow["Val"].ToString().Split(new char [] {';'}, StringSplitOptions.RemoveEmptyEntries));
                        }
                    }
                    break;
                case "ComboBox_Value":
                    break;
                default:
                    break;
            }
        }

        // Обрабатывает событие Получения выходных данных от запущенного процесса.
        // Получаем версию утилиты (нужно доработать, чтобы учитывались постфиксы, вроде dev, b и т.д.)
        private void ProcessForVerion_DataReceived(object sender, DataReceivedEventArgs e)
        {
            //string[] splittedOutput = e.Data.Split(',');
            if (!String.IsNullOrEmpty(e.Data))
            {
                Regex rgx = new Regex(@"\d+\.\d+\.\d");
                m_UtilityVersion = new Version(rgx.Match(e.Data).ToString());
            }
        }
        #endregion
    }
}
