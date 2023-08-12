using System;
using System.Collections.Generic;
using UnityEngine;

namespace Slimple.Core
{
    public abstract class Reference
    {
        private int m_ReferenceCount = 0;
        private bool m_Destroyed = false;
        
        public static implicit operator bool(Reference reference) => reference is {m_Destroyed: false};
        
        public void Retain()
        {
            Debug.Assert(!m_Destroyed);
            m_ReferenceCount++;
        }
        
        public void Release()
        {
            Debug.Assert(!m_Destroyed);
            m_ReferenceCount--;
            if (m_ReferenceCount != 0)
            {
                return;
            }
            m_Destroyed = true;
            OnDestroy();
            return;
        }

        public abstract void OnDestroy();
    }
}