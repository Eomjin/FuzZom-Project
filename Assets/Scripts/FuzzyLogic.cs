using System;
using System.Collections.Generic;
using UnityEngine;

public class FuzzyLogic
{
    public class FuzzyVariable
    {
        private Dictionary<string, FuzzySet> sets = new Dictionary<string, FuzzySet>();
        private List<Func<float, float>> rules = new List<Func<float, float>>();

        // 퍼지 집합 추가
        public void AddSet(string name, FuzzySet set)
        {
            sets[name] = set;
        }

        // 퍼지 규칙 추가
        public void AddRule(Func<float, float> ruleFunction)
        {
            rules.Add(ruleFunction);
        }

        // 입력값을 바탕으로 모든 규칙을 적용한 후 평균값 반환
        public float ApplyRules(float inputValue)
        {
            if (rules.Count == 0) return 0; // 예외 방지

            float result = 0;
            foreach (var rule in rules)
            {
                result += rule(inputValue);
            }
            return result / rules.Count;
        }

        // 특정 퍼지 집합의 멤버십 값을 반환
        public float Fuzzify(string setName, float value)
        {
            return sets.ContainsKey(setName) ? sets[setName].Membership(value) : 0;
        }

        // 특정 퍼지 집합의 디퍼지화 값 반환
        public float Defuzzify(string setName, float membership)
        {
            return sets.ContainsKey(setName) ? sets[setName].Centroid(membership) : 0;
        }

        // 여러 퍼지 집합에서 멤버십 값 반환 (FuzzifyAll 추가)
        public Dictionary<string, float> FuzzifyAll(float value)
        {
            Dictionary<string, float> memberships = new Dictionary<string, float>();
            foreach (var set in sets)
            {
                memberships[set.Key] = set.Value.Membership(value);
            }
            return memberships;
        }

        // 여러 퍼지 집합을 종합하여 디퍼지화 (DefuzzifyAll 추가)
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

        // 멤버십 함수 적용
        public float Membership(float value)
        {
            return membershipFunction(value);
        }

        // 퍼지 집합의 중심(센트로이드) 계산
        public float Centroid(float membership)
        {
            return (Min + Max) / 2 * membership;
        }
    }

    private Dictionary<string, FuzzyVariable> variables = new Dictionary<string, FuzzyVariable>();

    // 퍼지 변수 추가
    public FuzzyVariable AddVariable(string name)
    {
        var variable = new FuzzyVariable();
        variables[name] = variable;
        return variable;
    }

    // 퍼지 변수 가져오기
    public FuzzyVariable GetVariable(string name)
    {
        return variables.ContainsKey(name) ? variables[name] : null;
    }
}
