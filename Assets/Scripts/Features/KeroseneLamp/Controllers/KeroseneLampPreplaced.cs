using UnityEngine;

namespace ThatGameJam.Features.KeroseneLamp.Controllers
{
    public class KeroseneLampPreplaced : MonoBehaviour
    {
        [SerializeField] private string areaId;

        public string AreaId => areaId;
    }
}
