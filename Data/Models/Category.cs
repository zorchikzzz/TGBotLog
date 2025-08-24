using System;

namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// ������ ��������� ��������/�������
    /// ������������ ����� ������ ��� ����������� ���������� ��������
    /// </summary>
    public class Category
    {
        /// <summary>
        /// ���������� ������������� ���������
        /// ������������� ������������ ����� ������ ��� ����������
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// �������� ��������� (��������, "��������", "���������", "�����������")
        /// ������ ���� ���������� � �������
        /// </summary>
        public string Name { get; set; } = string.Empty;
    }
}