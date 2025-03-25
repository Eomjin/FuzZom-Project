using System;
using System.Collections.Generic;
using UnityEngine;

public class FuzzyLogic
{
    public class FuzzyVariable
    {
        private Dictionary<string, FuzzySet> sets = new Dictionary<string, FuzzySet>();
        private List<Func<float, float>> rules = new List<Func<float, float>>();

        // ���� ���� �߰�
        public void AddSet(string name, FuzzySet set)
        {
            sets[name] = set;
        }

        // ���� ��Ģ �߰�
        public void AddRule(Func<float, float> ruleFunction)
        {
            rules.Add(ruleFunction);
        }

        // �Է°��� �������� ��� ��Ģ�� ������ �� ��հ� ��ȯ
        public float ApplyRules(float inputValue)
        {
            if (rules.Count == 0) return 0; // ���� ����

            float result = 0;
            foreach (var rule in rules)
            {
                result += rule(inputValue);
            }
            return result / rules.Count;
        }

        // Ư�� ���� ������ ����� ���� ��ȯ
        public float Fuzzify(string setName, float value)
        {
            return sets.ContainsKey(setName) ? sets[setName].Membership(value) : 0;
        }

        // Ư�� ���� ������ ������ȭ �� ��ȯ
        public float Defuzzify(string setName, float membership)
        {
            return sets.ContainsKey(setName) ? sets[setName].Centroid(membership) : 0;
        }

        // ���� ���� ���տ��� ����� �� ��ȯ (FuzzifyAll �߰�)
        public Dictionary<string, float> FuzzifyAll(float value)
        {
            Dictionary<string, float> memberships = new Dictionary<string, float>();
            foreach (var set in sets)
            {
                memberships[set.Key] = set.Value.Membership(value);
            }
            return memberships;
        }

        // ���� ���� ������ �����Ͽ� ������ȭ (DefuzzifyAll �߰�)
        public float DefuzzifyAll(Dictionary<string, float> memberships)
        {
            float numerator = 0, denominator = 0;

            foreach (var set in sets)
            {
                float centroid = set.Value.Centroid(memberships[set.Key]);
                numerator += centroid * memberships[set.Key];
                denominator += memberships[set.Key];
            }

            return denominator > 0 ? numerator / denominator : 0;
        }
    }

    public class FuzzySet
    {
        public float Min { get; }
        public float Max { get; }
        private Func<float, float> membershipFunction;

        public FuzzySet(float min, float max, Func<float, float> membershipFunction)
        {
            Min = min;
            Max = max;
            this.membershipFunction = membershipFunction;
        }

        // ����� �Լ� ����
        public float Membership(float value)
        {
            return membershipFunction(value);
        }

        // ���� ������ �߽�(��Ʈ���̵�) ���
        public float Centroid(float membership)
        {
            return (Min + Max) / 2 * membership;
        }
    }

    private Dictionary<string, FuzzyVariable> variables = new Dictionary<string, FuzzyVariable>();

    // ���� ���� �߰�
    public FuzzyVariable AddVariable(string name)
    {
        var variable = new FuzzyVariable();
        variables[name] = variable;
        return variable;
    }

    // ���� ���� ��������
    public FuzzyVariable GetVariable(string name)
    {
        return variables.ContainsKey(name) ? variables[name] : null;
    }
}
