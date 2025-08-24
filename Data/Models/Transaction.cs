using System;

namespace FamilyBudgetBot.Data.Models
{
    /// <summary>
    /// ������ ���������� �������� (����������)
    /// ������������ ����� ������ � ������ ��� ������� �������
    /// </summary>
    public class Transaction
    {
        /// <summary>
        /// ���������� ������������� ����������
        /// ������������� ������������ ����� ������ ��� ����������
        /// </summary>
        public int Id { get; set; }

        /// <summary>
        /// ����� ���������� (������������� �����)
        /// ��� �������� ������ ������������ ������������� ��������
        /// </summary>
        public decimal Amount { get; set; }

        /// <summary>
        /// ���� � ����� ���������� ����������
        /// �� ��������� ������������ ������� ����� ��� ��������
        /// </summary>
        public DateTime Date { get; set; }

        /// <summary>
        /// ������������� ���������, � ������� ��������� ����������
        /// ��������� ���������� � ������������ ����������
        /// </summary>
        public int CategoryId { get; set; }

        /// <summary>
        /// �������������� �������� ��� ����������� � ����������
        /// ����� ��������� details � ���, �� ��� ������ ���� ��������� ��������
        /// </summary>
        public string Description { get; set; } = string.Empty;
    }
}